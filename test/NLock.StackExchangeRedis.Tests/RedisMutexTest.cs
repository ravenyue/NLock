using NLock.StackExchangeRedis.Locks;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace NLock.StackExchangeRedis.Tests
{
    public class RedisMutexTest
    {
        static object locker = new object();
        private readonly ConnectionMultiplexer _conn;
        private readonly IDatabase _redis;

        public RedisMutexTest()
        {
            _conn = ConnectionMultiplexer.Connect("192.168.50.100");
            _redis = _conn.GetDatabase();
        }

        [Fact]
        public async Task Should_be_order()
        {
            _redis.StringSet("count", 5);

            await CreateParallelTask(10, async () =>
            {
                var rlock = new RedisMutexLock(_conn, "locktest");
                await rlock.TryAcquireAsync(0);

                await DeductInventory();

                await rlock.ReleaseAsync();
            });

            var count = _redis.StringGet("count").ToString();

            Assert.Equal("0", count);
        }

        private Task CreateParallelTask(int count, Func<Task> func)
        {
            var tasks = new List<Task>();

            for (int i = 0; i < count; i++)
            {
                tasks.Add(Task.Run(func));
            }

            return Task.WhenAll(tasks);
        }

        private async Task DeductInventory()
        {
          
            var val = _redis.StringGet("count");
            var count = int.Parse(val.ToString());
            await Task.Delay(200);
            if (count > 0)
            {
                _redis.StringIncrement("count", -1);
            }
        }
    }
}
