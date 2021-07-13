using Medallion.Threading;
using Medallion.Threading.Redis;
using NLock.StackExchangeRedis.Locks;
using StackExchange.Redis;
using StackExchange.Redis.KeyspaceIsolation;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleRedisSample
{
    class Program
    {
        static object locker = new object();

        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            //var conn = ConnectionMultiplexer.Connect("192.168.50.100");
            //var redis = conn.GetDatabase();

            DistLock();
            Console.WriteLine("======执行完成======");
        }

        static void DistLock()
        {
            var connection = ConnectionMultiplexer.Connect("192.168.66.60"); // uses StackExchange.Redis

            connection.GetDatabase().WithKeyPrefix("lock");
            IDistributedLock dlock = new RedisDistributedLock("MyLockName", connection.GetDatabase(2));
            using (var handle = dlock.TryAcquire())
            {
                if (handle != null) 
                {
                    Console.WriteLine("Locked");
                    Thread.Sleep(TimeSpan.FromSeconds(60));
                    /* I have the lock */
                    Console.WriteLine("Unlocked");
                }
            }
        }


        static async Task Test()
        {
            var conn = ConnectionMultiplexer.Connect("192.168.50.100");
            var redis = conn.GetDatabase();


            var tasks = new List<Task>();


            for (int i = 0; i < 10; i++)
            {
                var t = Task.Run(async () =>
                {
                    var rlock = new RedisMutexLock(conn, "locktest");
                    //Console.WriteLine($"开始执行{i}");
                    await rlock.TryAcquireAsync(0);
                    Console.WriteLine("加锁成功");

                    //await Task.Delay(2000);

                    await rlock.ReleaseAsync();
                    Console.WriteLine("解锁成功");
                });
                tasks.Add(t);
            }

            await Task.WhenAll(tasks);
        }


        static async Task LockTest()
        {
            var conn = ConnectionMultiplexer.Connect("192.168.50.100");
            var redis = conn.GetDatabase();
            redis.StringSet("count", 50);
            var tasks = new List<Task>();

            for (int i = 0; i < 100; i++)
            {
                var t = Task.Run(async () =>
               {
                   var rlock = new RedisMutexLock(conn, "locktest");
                   await rlock.TryAcquireAsync(0);
                   Console.WriteLine($"线程{Thread.CurrentThread.ManagedThreadId}获取锁");
                   var val = redis.StringGet("count");
                   var count = int.Parse(val.ToString());

                    //await Task.Delay(500);
                    if (count > 0)
                   {
                       redis.StringIncrement("count", -1);
                   }

                   await rlock.ReleaseAsync();
                   Console.WriteLine($"线程{Thread.CurrentThread.ManagedThreadId}释放锁");
               });
                tasks.Add(t);
            }

            await Task.WhenAll(tasks);
            Console.WriteLine("=====================");
            Console.WriteLine(redis.StringGet("count"));

            Console.ReadKey();

        }
    }
}
