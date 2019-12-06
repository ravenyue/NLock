using NLock.Core;
using org.apache.zookeeper;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NLock.Zookeeper
{
    public class StandardLockInternalsDriver : ILockInternalsDriver
    {
        /// <summary>
        /// 创建锁的临时有序节点
        /// </summary>
        /// <param name="zkClient"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public async virtual Task<string> CreateLockNodeAsync(ZooKeeper zkClient, string path)
        {
            var ourPath = await zkClient
                .RecursionCreateAsync(path, null, ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.EPHEMERAL_SEQUENTIAL);

            return ourPath;
        }

        public virtual PredicateResults GetTheLock(ZooKeeper client, List<string> sortedChildren, string sequenceNodePath, int maxLeases)
        {
            var sequenceNodeName = ZKPaths.GetNodeFromPath(sequenceNodePath);
            var ourIndex = sortedChildren.IndexOf(sequenceNodeName);
            if (ourIndex < 0)
            {
                throw new KeeperException.NoNodeException(sequenceNodePath);
            }

            var locked = ourIndex < maxLeases;
            var nodeToWatch = locked ? null : sortedChildren[ourIndex - maxLeases];

            return new PredicateResults(nodeToWatch, locked);
        }

        /// <summary>
        /// 获取节点排序的部分
        /// </summary>
        /// <param name="str"></param>
        /// <param name="lockName"></param>
        /// <returns></returns>
        public virtual string FixForSorting(string str, string lockName)
        {
            var index = str.LastIndexOf(lockName);
            if (index >= 0)
            {
                index += lockName.Length;
                return index <= str.Length ? str.Substring(index) : "";
            }
            return str;
        }
    }
}