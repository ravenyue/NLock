using Microsoft.Extensions.Options;
using NLock.Core;
using NLock.Core.Locks;
using org.apache.zookeeper;
using System;
using System.Collections.Generic;
using System.Text;

namespace NLock.Zookeeper
{
    public class ZookeeperLockFactory : IDistributedLockFactory
    {
        private readonly ZooKeeper _zkClient;
        private readonly ZookeeperOptions _options;

        private const string BASE_LOCK_PATH = "/n_lock";

        public ZookeeperLockFactory(ZookeeperOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _zkClient = new ZooKeeper(
                _options.ConnectionString,
                _options.SessionTimeout,
                _options.Watcher);
        }

        public ZookeeperLockFactory(IOptions<ZookeeperOptions> options)
            : this(options.Value)
        {
        }

        //public ZookeeperLockProvider(ZooKeeper zkClient)
        //{
        //    _zkClient = zkClient;
        //}

        //public ZookeeperLockProvider(string connectString, int sessionTimeout)
        //{
        //    _zkClient = new ZooKeeper(connectString, sessionTimeout, null);
        //}

        public IDistributedLock CreateMutexLock(string name)
        {
            var path = ZKPaths.MakePath(BASE_LOCK_PATH, name);
            var mlock = new ZookeeperMutexLock(_zkClient, path, _options.DefaultLockTimeout);

            return mlock;
        }

        public IDistributedReadWriteLock CreateReadWriteLock(string name)
        {
            var path = ZKPaths.MakePath(BASE_LOCK_PATH, name);
            var rwlock = new ZookeeperReadWriteLock(_zkClient, path, _options.DefaultLockTimeout);

            return rwlock;
        }

        public IDistributedSemaphore CreateSemaphore(string name, int maxLeases)
        {
            throw new NotImplementedException();
        }
    }
}
