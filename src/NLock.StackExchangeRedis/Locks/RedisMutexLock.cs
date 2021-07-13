using DotNetty.Common.Utilities;
using NLock.Core.Locks;
using NLock.StackExchangeRedis.Utils;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace NLock.StackExchangeRedis.Locks
{
    public class RedisMutexLock : IDistributedLock
    {
        private const string Prefix = "nlock";
        private readonly LockPubSub _lockPubSub;
        private readonly string _entryName;

        protected readonly IDatabase RedisDb;
        protected readonly int InternalLockLeaseTime;

        private static ConcurrentDictionary<string, ExpirationEntry> _expirationRenewalMap =
            new ConcurrentDictionary<string, ExpirationEntry>();

        public RedisMutexLock(
            ConnectionMultiplexer connection,
            string name)
        {
            Name = name;
            LockId = Guid.NewGuid().ToString();
            RedisDb = connection.GetDatabase();
            InternalLockLeaseTime = 30000;

            _lockPubSub = new LockPubSub(connection);
            _entryName = $"{LockId}:{name}";
        }

        public string Name { get; }
        public virtual string LockId { get; protected set; }

        public virtual string LockKey => string.Concat(Prefix, ":", Name);

        public virtual string ChannelName => string.Concat(Prefix, "_channel:", Name);

        public async Task<bool> TryAcquireAsync(int millisecondsTimeout)
        {
            var startTime = TimeoutHelper.GetTime();

            var ttl = await TryAcquireInnerAsync(InternalLockLeaseTime);

            // 获得锁成功
            if (!ttl.HasValue)
                return true;

            if (TimeoutHelper.IsTimeout(startTime, millisecondsTimeout))
                return false;

            var awaitTime = CalcAwaitTime(startTime, millisecondsTimeout, ttl.Value);
            // 等待锁释放消息
            await _lockPubSub.SubscribeAsync(ChannelName, awaitTime);

            try
            {
                while (true)
                {
                    if (TimeoutHelper.IsTimeout(startTime, millisecondsTimeout))
                        return false;

                    ttl = await TryAcquireInnerAsync(InternalLockLeaseTime);
                    // 获得锁成功
                    if (!ttl.HasValue)
                        return true;

                    if (TimeoutHelper.IsTimeout(startTime, millisecondsTimeout))
                        return false;

                    awaitTime = CalcAwaitTime(startTime, millisecondsTimeout, ttl.Value);
                    // 等待锁释放消息
                    await _lockPubSub.Latch.WaitAsync(awaitTime);
                }
            }
            finally
            {
                await _lockPubSub.UnSubscribeAsync(ChannelName);
            }
        }

        private int CalcAwaitTime(uint startTime, int originalWaitMillisecondsTimeout, int ttl)
        {
            if (originalWaitMillisecondsTimeout == Timeout.Infinite)
            {
                return ttl;
            }

            return Math.Min(ttl, TimeoutHelper.UpdateTimeOut(startTime, originalWaitMillisecondsTimeout));
        }

        private Task<int?> TryAcquireInnerAsync(int leaseTime)
        {
            return InvokLockScriptAsync(leaseTime)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        ExceptionDispatchInfo.Capture(t.Exception.InnerException).Throw();
                    }
                    if (!t.Result.HasValue)
                    {
                        ScheduleExpirationRenewal();
                    }
                    return t.Result;
                });
        }

        protected virtual async Task<int?> InvokLockScriptAsync(int leaseTime)
        {
            var preparedScript = LuaScriptLoader.GetScript(LockScript.MUTEX_LOCK_ACQUIRE);

            var result = await RedisDb.ScriptEvaluateAsync(preparedScript, new
            {
                lock_key = LockKey,
                lock_id = LockId,
                expire = leaseTime,
            });

            if (result.IsNull) return null;

            return int.Parse(result.ToString());
        }

        public Task ReleaseAsync()
        {
            return InvokUnlockScriptAsync(InternalLockLeaseTime)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        ExceptionDispatchInfo.Capture(t.Exception.InnerException).Throw();
                    }
                    CancelExpirationRenewal();
                });
        }

        private void CancelExpirationRenewal()
        {
            var entry = _expirationRenewalMap[_entryName];

            if (entry == null)
            {
                return;
            }

            if (entry.ReduceCount() <= 0)
            {
                //entry.Timeout.Cancel();
                entry.Timer?.Dispose();
                _expirationRenewalMap.TryRemove(_entryName, out _);
            }
        }

        protected virtual async Task InvokUnlockScriptAsync(int leaseTime)
        {
            var preparedScript = LuaScriptLoader.GetScript(LockScript.MUTEX_LOCK_RELEASE);

            var result = await RedisDb.ScriptEvaluateAsync(preparedScript, new
            {
                lock_key = LockKey,
                lock_id = LockId,
                expire = leaseTime,
                channel = ChannelName,
                msg = LockPubSub.UNLOCK_MESSAGE,
            });
        }

        private void ScheduleExpirationRenewal()
        {
            var newEntry = new ExpirationEntry();
            var entry = _expirationRenewalMap.GetOrAdd(_entryName, newEntry);
            if (entry != newEntry)
            {
                entry.IncreaseCount();
            }
            else
            {
                entry.IncreaseCount();
                RenewExpiration();
            }
        }

        private void RenewExpiration()
        {
            var entry = _expirationRenewalMap[_entryName];

            if (entry == null)
            {
                return;
            }

            //var task = new HashedWheelTimer()
            //    .NewTimeout(new ActionTimerTask(async timeout =>
            //    {
            //        var ent = ExpirationRenewalMap[_entryName];
            //        if (ent == null)
            //        {
            //            return;
            //        }

            //        var res = await RenewExpirationAsync(_internalLockLeaseTime/3);
            //        if (res)
            //        {
            //            RenewExpiration();
            //        }

            //    }), TimeSpan.FromSeconds(_internalLockLeaseTime));

            var period = InternalLockLeaseTime / 3;
            var timer = new Timer(async state =>
            {
                var ent = _expirationRenewalMap[_entryName];
                if (ent == null)
                {
                    return;
                }

                var res = await RenewExpirationAsync();
                //if (res)
                //{
                //    RenewExpiration();
                //}
            }, null, period, period);

            //entry.Timeout = task;
            entry.Timer = timer;
        }

        protected virtual async Task<bool> RenewExpirationAsync()
        {
            var preparedScript = LuaScriptLoader.GetScript(LockScript.MUTEX_LOCK_RENEW);

            var result = await RedisDb.ScriptEvaluateAsync(preparedScript, new
            {
                key = LockKey,
                field = LockId,
                expire = InternalLockLeaseTime,
            });

            return string.Equals("1", result.ToString());
        }

        public bool TryAcquire(int millisecondsTimeout)
        {
            return TryAcquireAsync(millisecondsTimeout)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        public void Release()
        {
            ReleaseAsync()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        private class ExpirationEntry
        {
            public ITimeout Timeout { get; set; }
            public Timer Timer { get; set; }

            private int counter;

            public int IncreaseCount()
            {
                return ++counter;
            }

            public int ReduceCount()
            {
                return --counter;
            }

        }
    }
}
