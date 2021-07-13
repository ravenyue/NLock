using NLock.StackExchangeRedis.Utils;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NLock.StackExchangeRedis.Locks
{
    public class RedisWriteLock : RedisMutexLock
    {
        public RedisWriteLock(
            ConnectionMultiplexer connection,
            string name)
            : base(connection, name)
        {
        }

        public override string ChannelName => string.Concat("nlock_rwlock", LockKey);

        public override string LockId => string.Concat(base.LockId, ":write");

        protected override async Task<int?> InvokLockScriptAsync(int leaseTime)
        {
            var preparedScript = LuaScriptLoader.GetScript(LockScript.WRITE_LOCK_ACQUIRE);

            var result = await RedisDb.ScriptEvaluateAsync(preparedScript, new
            {
                lock_key = LockKey,
                expire = leaseTime,
                lock_id = LockId,
            });

            if (result.IsNull) return null;

            return int.Parse(result.ToString());
        }

        protected override async Task InvokUnlockScriptAsync(int leaseTime)
        {
            var preparedScript = LuaScriptLoader.GetScript(LockScript.WRITE_LOCK_RELEASE);

            var result = await RedisDb.ScriptEvaluateAsync(preparedScript, new
            {
                lock_key = LockKey,
                channel = ChannelName,
                read_unlock_msg = LockPubSub.READ_UNLOCK_MESSAGE,
                expire = leaseTime,
                lock_id = LockId
            });
        }
    }
}
