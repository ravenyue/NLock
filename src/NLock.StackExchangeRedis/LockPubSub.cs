using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NLock.StackExchangeRedis
{
    public class LockPubSub
    {
        public const int UNLOCK_MESSAGE = 0;
        public const int READ_UNLOCK_MESSAGE = 1;

        private readonly ISubscriber _pubSub;

        public SemaphoreSlim Latch { get; }

        public LockPubSub(ConnectionMultiplexer connection)
        {
            _pubSub = connection.GetSubscriber();
            Latch = new SemaphoreSlim(0,1);
        }

        public async Task<bool> SubscribeAsync(string channelName, int millisecondsTimeout)
        {
            _pubSub.Subscribe(channelName, UnLockMessageHandle);

            return await Latch.WaitAsync(millisecondsTimeout);
        }

        public Task UnSubscribeAsync(string channelName)
        {
            return _pubSub.UnsubscribeAsync(channelName, UnLockMessageHandle);
        }

        public void UnLockMessageHandle(RedisChannel channel, RedisValue message)
        {
            Console.WriteLine($"=======释放锁通知{message}======");
            if (message.TryParse(out int unlock) && unlock.Equals(UNLOCK_MESSAGE))
            {
                Latch.Release();
            }
        }
    }
}
