using NLock.StackExchangeRedis.Utils;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NLock.StackExchangeRedis.Locks
{
    public class RedisReadLock : RedisMutexLock
    {
        public RedisReadLock(
            ConnectionMultiplexer connection,
            string name) 
            : base(connection, name)
        {
        }

        public override string ChannelName => string.Concat("nlock_rwlock", LockKey);

        private string GetReadWriteTimeoutNamePrefix()
        {
            return string.Concat(LockKey, ":", LockId, ":rwlock_timeout");
        }

        private string GetWriteLockName() => string.Concat(LockId, ":write");

        protected override async Task<int?> InvokLockScriptAsync(int leaseTime)
        {
            var preparedScript = LuaScriptLoader.GetScript(LockScript.READ_LOCK_ACQUIRE);

            var result = await RedisDb.ScriptEvaluateAsync(preparedScript, new
            {
                lock_key = LockKey,
                timeout_key = GetReadWriteTimeoutNamePrefix(),
                expire = leaseTime,
                lock_id = LockId,
                write_lock = GetWriteLockName()
            });

            if (result.IsNull) return null;

            return int.Parse(result.ToString());
        }

        protected override async Task InvokUnlockScriptAsync(int leaseTime)
        {
            var preparedScript = LuaScriptLoader.GetScript(LockScript.READ_LOCK_RELEASE);

            var result = await RedisDb.ScriptEvaluateAsync(preparedScript, new
            {
                lock_key = LockKey,
                channel = ChannelName,
                timeout_key = GetReadWriteTimeoutNamePrefix(),
                timeout_prefix = LockKey,
                unlock_msg = LockPubSub.UNLOCK_MESSAGE,
                lock_id = LockId
            });
        }

        protected override async Task<bool> RenewExpirationAsync()
        {
            var preparedScript = LuaScriptLoader.GetScript(LockScript.READ_LOCK_RENEW);

            var result = await RedisDb.ScriptEvaluateAsync(preparedScript, new
            {
                lock_key = LockKey,
                timeout_prefix = LockKey,
                expire= InternalLockLeaseTime,
                lock_id = LockId
            });

            return string.Equals("1", result.ToString());
        }
    }
}
