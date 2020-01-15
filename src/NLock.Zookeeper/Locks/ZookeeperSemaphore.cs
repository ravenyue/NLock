using NLock.Core;
using NLock.Core.Locks;
using org.apache.zookeeper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NLock.Zookeeper
{
    public class ZookeeperSemaphore : IDistributedSemaphore
    {
        private readonly ZooKeeper _zkClient;
        private readonly int _maxLeases;
        private readonly int _lockTimeout;
        private readonly ZookeeperMutexLock _lock;
        private readonly string _leasesPath;
        private readonly Watcher _watcher;
        private readonly SemaphoreSlim _signal;
        private readonly List<string> _acquiredLeases;

        private const string LOCK_PARENT = "locks";
        private const string LEASE_PARENT = "leases";
        private const string LEASE_BASE_NAME = "lease-";

        public int AcquiredCount => throw new NotImplementedException();

        /// <summary>
        /// zk信号量
        /// </summary>
        /// <param name="zkClient"></param>
        /// <param name="path"></param>
        /// <param name="maxLeases"></param>
        public ZookeeperSemaphore(ZooKeeper zkClient, string path, int maxLeases, int lockTimeout)
        {
            ZKPaths.ValidatePath(path);

            _zkClient = zkClient;
            _maxLeases = maxLeases;
            _lockTimeout = lockTimeout;
            _lock = new ZookeeperMutexLock(zkClient, ZKPaths.MakePath(path, LOCK_PARENT), lockTimeout);
            _leasesPath = ZKPaths.MakePath(path, LEASE_PARENT);
            _acquiredLeases = new List<string>(maxLeases);

            _signal = new SemaphoreSlim(0);
            _watcher = new ReleaseLockWatcher(_signal);
        }

        public void AcquireMultiple(int quantity)
        {
            throw new NotImplementedException();
        }

        public void AcquireMultiple(int quantity, int millisecondsTimeout)
        {
            throw new NotImplementedException();
        }

        public void AcquireMultiple(int quantity, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public bool TryAcquireMultiple(int quantity)
        {
            throw new NotImplementedException();
        }

        public bool TryAcquireMultiple(int quantity, int millisecondsTimeout)
        {
            throw new NotImplementedException();
        }

        public bool TryAcquireMultiple(int quantity, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public void Release(int quantity)
        {
            throw new NotImplementedException();
        }

        public void ReleaseAll()
        {
            throw new NotImplementedException();
        }

        public Task AcquireMultipleAsync(int quantity)
        {
            throw new NotImplementedException();
        }

        public Task AcquireMultipleAsync(int quantity, int millisecondsTimeout)
        {
            throw new NotImplementedException();
        }

        public Task AcquireMultipleAsync(int quantity, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public void Acquire()
        {
            throw new NotImplementedException();
        }

        public void Acquire(int millisecondsTimeout)
        {
            throw new NotImplementedException();
        }

        public void Acquire(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public bool TryAcquire()
        {
            throw new NotImplementedException();
        }

        public bool TryAcquire(int millisecondsTimeout)
        {
            throw new NotImplementedException();
        }

        public bool TryAcquire(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public void Release()
        {
            throw new NotImplementedException();
        }

        public Task AcquireAsync(int millisecondsTimeout)
        {
            throw new NotImplementedException();
        }



        public Task AcquireAsync()
        {
            return AcquireMultipleAsync(1, _lockTimeout);
        }

        public Task AcquireAsync(TimeSpan timeout)
        {
            return InternalAcquireAsync(1, (int)timeout.TotalMilliseconds);
        }

        public Task<bool> TryAcquireAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> TryAcquireAsync(int millisecondsTimeout)
        {
            throw new NotImplementedException();
        }

        public Task<bool> TryAcquireAsync(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }



        private async Task InternalAcquireAsync(int quantity, int timeout)
        {
            if (quantity <= 0)
            {
                throw new ArgumentException("quantity不能小于0", nameof(quantity));
            }

            var startMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var waitMs = timeout;
            var success = false;
            var leases = new List<string>(quantity);

            // 获取Leases节点数据，不存在则创建
            var count = await GetOrCreateLeasesNode(_leasesPath, _maxLeases);

            if (quantity > count)
            {
                throw new ArgumentException("quantity不能大于maxLeases", nameof(quantity));
            }

            try
            {
                while (quantity-- > 0)
                {
                    long startMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    // 获取一个Lease
                    var leasePath = await InternalAcquireOneLeaseAsync(startMs, waitMs);
                    leases.Add(leasePath);
                }
                _acquiredLeases.AddRange(leases);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    // 删除已创建的lease节点
                    await DeleteAllLease(leases);
                }
            }
        }

        /// <summary>
        /// 获取一个Lease
        /// </summary>
        /// <param name="startMs">开始毫秒数</param>
        /// <param name="waitMs">等待毫秒数</param>
        /// <returns>Lease节点名称</returns>
        private async Task<string> InternalAcquireOneLeaseAsync(long startMs, long waitMs)
        {
            // 获取剩余等待时间
            var remainingTime = GetRemainingWaitMs(startMs, waitMs);
            if (remainingTime <= 0)
            {
                new TimeoutException("等待超时");
            }
            // 获取分布式锁
            await _lock.AcquireAsync(remainingTime);

            string leasePath;
            var success = false;

            try
            {
                // 创建Lease临时有序节点
                leasePath = await _zkClient.createAsync(ZKPaths.MakePath(_leasesPath, LEASE_BASE_NAME), null,
                   ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.EPHEMERAL_SEQUENTIAL);
                // 节点名称
                var leaseNodeName = ZKPaths.GetNodeFromPath(leasePath);

                try
                {
                    while (true)
                    {
                        var childrenResult = await _zkClient.getChildrenAsync(_leasesPath, _watcher);

                        // 找不到刚创建的Lease节点
                        if (!childrenResult.Children.Contains(leaseNodeName))
                        {
                            throw new KeeperException.NoNodeException("Sequential path not found - possible session loss");
                        }

                        // 成功获取一个Lease
                        if (childrenResult.Children.Count <= _maxLeases)
                        {
                            break;
                        }

                        // 剩余等待时间
                        remainingTime = GetRemainingWaitMs(startMs, waitMs);
                        if (remainingTime <= 0)
                        {
                            throw new TimeoutException("等待超时");
                        }

                        // 等待被唤醒
                        var result = await _signal.WaitAsync(remainingTime);

                        if (!result)
                        {
                            throw new TimeoutException("等待超时");
                        }
                    }
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        await DeleteLease(leasePath);
                    }
                }
            }
            finally
            {
                await _lock.ReleaseAsync();
            }

            return leasePath;
        }

        public async Task ReleaseAsync()
        {
            if (_acquiredLeases.Count == 0)
            {
                return;
            }
            await DeleteLease(_acquiredLeases.First());
            _acquiredLeases.RemoveAt(0);
        }

        public async Task ReleaseAsync(int quantity)
        {
            if (_acquiredLeases.Count == 0)
            {
                return;
            }

            quantity = Math.Min(quantity, _acquiredLeases.Count);
            for (int i = 0; i < quantity; i++)
            {
                await DeleteLease(_acquiredLeases[i]);
                _acquiredLeases.RemoveAt(i);
            }

        }

        public async Task ReleaseAllAsync()
        {
            if (_acquiredLeases.Count == 0)
            {
                return;
            }
            await DeleteAllLease(_acquiredLeases);
            _acquiredLeases.Clear();
        }


        private async Task<int> GetOrCreateLeasesNode(string leasesPath, int maxLeases)
        {
            try
            {
                var count = await _zkClient.GetDateIntAsync(leasesPath);
                if (count != maxLeases)
                {
                    throw new ApplicationException("maxLeases与Zookeeper中现有Semaphore节点数据不一致");
                }
                return count;
            }
            catch (KeeperException.NoNodeException)
            {
                // 创建Leases节点
                var data = Encoding.UTF8.GetBytes(maxLeases.ToString());
                await _zkClient.RecursionCreateAsync(leasesPath, data, CreateMode.PERSISTENT);
            }

            return maxLeases;
        }

        /// <summary>
        /// 获取剩余等待毫秒数
        /// </summary>
        /// <param name="startMs">开始时间戳</param>
        /// <param name="waitMs">总等待毫秒数</param>
        /// <returns></returns>
        private int GetRemainingWaitMs(long startMs, long waitMs)
        {
            long elapsedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - startMs;
            return (int)(waitMs - elapsedMs);
        }

        private async Task DeleteLease(string leasePath)
        {
            try
            {
                await _zkClient.deleteAsync(leasePath);
            }
            catch (KeeperException.NoNodeException)
            { }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task DeleteAllLease(IEnumerable<string> leases)
        {
            foreach (var path in leases)
            {
                await DeleteLease(path);
            }
        }


        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
