using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using org.apache.zookeeper;

namespace NLock.Zookeeper
{
    public interface ILockInternalsDriver : ILockInternalsSorter
    {
        /// <summary>
        /// 创建锁节点
        /// </summary>
        /// <param name="zkClient"></param>
        /// <param name="path"></param>
        /// <returns>锁节点路径</returns>
        Task<string> CreateLockNodeAsync(ZooKeeper zkClient, string path);

        /// <summary>
        /// 获取锁
        /// </summary>
        /// <param name="client"></param>
        /// <param name="sortedChildren">已排序的锁节点</param>
        /// <param name="sequenceNodePath">当前序列节点路径</param>
        /// <param name="maxLeases">最大租赁</param>
        /// <returns></returns>
        PredicateResults GetTheLock(ZooKeeper client, List<string> sortedChildren, string sequenceNodePath, int maxLeases);
    }
}
