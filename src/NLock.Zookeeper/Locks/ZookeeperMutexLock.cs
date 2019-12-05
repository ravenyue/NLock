using NLock.Core.Locks;
using org.apache.zookeeper;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace NLock.Zookeeper
{
    public class ZookeeperMutexLock : IDistributedLock
    {
        private const string LOCK_NAME = "lock-";

        private readonly LockInternals _internals;
        private readonly string _basePath;
        private readonly int _lockTimeout;
        private LockData _lockData;

        public ZookeeperMutexLock(ZooKeeper zkClient, string path, int lockTimeout)
            : this(zkClient, path, new StandardLockInternalsDriver(), lockTimeout)
        { }

        public ZookeeperMutexLock(ZooKeeper zkClient, string path, ILockInternalsDriver driver, int lockTimeout)
            : this(zkClient, path, LOCK_NAME, 1, driver, lockTimeout)
        { }

        public ZookeeperMutexLock(ZooKeeper zkClient, string path, string lockName, int maxLeases, ILockInternalsDriver driver, int lockTimeout)
        {
            _basePath = path;
            _lockTimeout = lockTimeout;
            _internals = new LockInternals(zkClient, driver, path, lockName, maxLeases);
        }

        #region Acquire

        public void Acquire()
        {
            AcquireAsync()
                .GetAwaiter().GetResult();
        }

        public void Acquire(TimeSpan timeout)
        {
            AcquireAsync(timeout)
                .GetAwaiter().GetResult();
        }

        public void Acquire(int millisecondsTimeout)
        {
            AcquireAsync(millisecondsTimeout)
                .GetAwaiter().GetResult();
        }

        public Task AcquireAsync()
        {
            return AcquireAsync(_lockTimeout);
        }

        public Task AcquireAsync(TimeSpan timeout)
        {
            return AcquireAsync((int)timeout.TotalMilliseconds);
        }

        public async Task AcquireAsync(int millisecondsTimeout)
        {
            var locked = await InternalLockAsync(millisecondsTimeout);

            if (!locked)
            {
                throw new TimeoutException("Acquire lock timeout");
            }
        }

        #endregion

        #region TryAcquire

        public bool TryAcquire()
        {
            return TryAcquireAsync()
                .GetAwaiter().GetResult();
        }

        public bool TryAcquire(int millisecondsTimeout)
        {
            return TryAcquireAsync(millisecondsTimeout)
                .GetAwaiter().GetResult();
        }

        public bool TryAcquire(TimeSpan timeout)
        {
            return TryAcquireAsync(timeout)
                .GetAwaiter().GetResult();
        }

        public Task<bool> TryAcquireAsync()
        {
            return TryAcquireAsync(_lockTimeout);
        }

        public Task<bool> TryAcquireAsync(TimeSpan timeout)
        {
            return TryAcquireAsync((int)timeout.TotalMilliseconds);
        }

        public Task<bool> TryAcquireAsync(int millisecondsTimeout)
        {
            return InternalLockAsync(millisecondsTimeout);
        }

        #endregion


        public void Release()
        {
            ReleaseAsync().GetAwaiter().GetResult();
        }

        public async Task ReleaseAsync()
        {
            if (_lockData == null)
            {
                throw new ApplicationException("You do not own the lock: " + _basePath);
            }

            int newLockCount = _lockData.CountDecrement();
            if (newLockCount > 0)
            {
                return;
            }
            if (newLockCount < 0)
            {
                throw new ApplicationException("Lock count has gone negative for lock: " + _basePath);
            }
            try
            {
                // 释放锁
                await _internals.ReleaseLockAsync(_lockData.LockPath);
            }
            finally
            {
                _lockData = null;
            }

        }

        /// <summary>
        /// 获取锁
        /// </summary>
        /// <param name="timeout">超时时间(毫秒)</param>
        /// <returns>成功返回true,超时返回false</returns>
        private async Task<bool> InternalLockAsync(int timeout)
        {
            if (_lockData != null)
            {
                _lockData.CountIncrement();
                return true;
            }

            // 尝试获取锁
            var lockPath = await _internals.AttemptLockAsync(timeout);

            // 成功获取锁
            if (!string.IsNullOrEmpty(lockPath))
            {
                _lockData = new LockData(lockPath);
                return true;
            }

            return false;
        }

        public bool IsAcquiredLock()
        {
            return _lockData != null;
        }

        public void Dispose()
        {
            _internals.Dispose();
        }
    }
}
