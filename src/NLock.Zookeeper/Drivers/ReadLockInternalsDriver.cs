using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NLock.Core;
using org.apache.zookeeper;

namespace NLock.Zookeeper
{
    public class ReadLockInternalsDriver : StandardLockInternalsDriver
    {
        private readonly ZookeeperMutexLock _writeLock;

        public ReadLockInternalsDriver(ZookeeperMutexLock writeLock)
        {
            _writeLock = writeLock;
        }

        public override PredicateResults GetTheLock(ZooKeeper client, List<string> sortedChildren, string sequenceNodeName, int maxLeases)
        {
            return ReadLockPredicate(sortedChildren, sequenceNodeName);
        }

        private PredicateResults ReadLockPredicate(List<string> children, string sequenceNodePath)
        {
            // 已获取写锁可以直接获取读锁
            if (_writeLock.IsAcquiredLock())
            {
                return new PredicateResults(null, true);
            }

            int watchWriteIndex = int.MaxValue;
            int ourIndex = -1;
            var sequenceNodeName = ZKPaths.GetNodeFromPath(sequenceNodePath);

            for (int index = 0; index < children.Count; index++)
            {
                var node = children[index];

                // 写锁节点
                if (node.Contains(ZookeeperReadWriteLock.WRITE_LOCK_NAME))
                {
                    watchWriteIndex = index;
                }
                // 当前读锁节点
                else if (node == sequenceNodeName)
                {
                    ourIndex = index;
                    break;
                }
            }

            if (ourIndex < 0)
            {
                throw new KeeperException.NoNodeException(sequenceNodePath);
            }

            var locked = (ourIndex < watchWriteIndex);
            var pathToWatch = locked ? null : children[watchWriteIndex];

            return new PredicateResults(pathToWatch, locked);
        }

        public override string FixForSorting(string str, string lockName)
        {
            str = base.FixForSorting(str, ZookeeperReadWriteLock.READ_LOCK_NAME);
            str = base.FixForSorting(str, ZookeeperReadWriteLock.WRITE_LOCK_NAME);

            return str;
        }
    }
}
