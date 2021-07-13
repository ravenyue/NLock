using NLock.StackExchangeRedis.Utils;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NLock.StackExchangeRedis.Locks
{
    public class RedisSemaphore
    {
        private const string PREFIX = "nlock";

        private readonly int _internalLockLeaseTime;
        private readonly string _name;
        private readonly IDatabase _redisDb;

        public RedisSemaphore(
            ConnectionMultiplexer connection,
            LockPubSub lockPubSub,
            string name)
        {
            //_id = Guid.NewGuid().ToString();
            _redisDb = connection.GetDatabase();
            //_lockPubSub = lockPubSub;
            _internalLockLeaseTime = 10000;
            _name = name;
        }

        public string GetLockKey() => string.Concat(PREFIX, ":", _name);

        public virtual string GetChannelName() => string.Concat(PREFIX, "_sc:", _name);

        public Task<bool> AcquireAsync(TimeSpan timeout)
        {
            return AcquireAsync(1, timeout);
        }

        public Task<bool> AcquireAsync(int quantity, TimeSpan timeout)
        {
            return TryAcquireAsync(quantity, (int)timeout.TotalMilliseconds);
        }



        public async Task<bool> TryAcquireAsync(int quantity, int millisecondsTimeout)
        {
            var preparedScript = LuaScriptLoader.GetScript(LockScript.SEMAPHORE_ACQUIRE);

            var result = await _redisDb.ScriptEvaluateAsync(preparedScript, new
            {
                lock_key = GetLockKey(),
                quantity = quantity,
            });

            return string.Equals("1", result.ToString());
        }


        public async Task ReleaseAsync(int permits)
        {
            var preparedScript = LuaScriptLoader.GetScript(LockScript.SEMAPHORE_RELEASE);

            var result = await _redisDb.ScriptEvaluateAsync(preparedScript, new
            {
                lock_key = GetLockKey(),
                channel = GetChannelName(),
                permits = permits,
            });
        }
    }
}
