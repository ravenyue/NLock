using NLock.Core.Locks;
using org.apache.zookeeper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NLock.Zookeeper
{
    /// <summary>
    /// zk读写锁
    /// </summary>
    public class ZookeeperReadWriteLock : IDistributedReadWriteLock
    {
        public const string READ_LOCK_NAME = "__READ__";
        public const string WRITE_LOCK_NAME = "__WRIT__";

        private readonly ZookeeperMutexLock _readMutex;
        private readonly ZookeeperMutexLock _writeMutex;

        public ZookeeperReadWriteLock(ZooKeeper zkClient, string path, int lockTimeout)
        {
            _writeMutex = new ZookeeperMutexLock
            (
                zkClient,
                path,
                WRITE_LOCK_NAME,
                1,
                new WriteLockInternalsDriver(),
                lockTimeout
            );

            _readMutex = new ZookeeperMutexLock
            (
                zkClient,
                path,
                READ_LOCK_NAME,
                int.MaxValue,
                new ReadLockInternalsDriver(_writeMutex),
                lockTimeout
            );
        }

        public IDistributedLock ReadLock()
        {
            return _readMutex;
        }

        public IDistributedLock WriteLock()
        {
            return _writeMutex;
        }
    }
}
