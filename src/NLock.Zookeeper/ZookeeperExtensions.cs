using org.apache.zookeeper;
using org.apache.zookeeper.data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NLock.Zookeeper
{
    public static class ZookeeperExtensions
    {
        /// <summary>
        /// 递归创建目录
        /// </summary>
        /// <param name="zkClient"></param>
        /// <param name="path"></param>
        /// <param name="createMode"></param>
        /// <returns></returns>
        public static Task<string> RecursionCreateAsync(this ZooKeeper zkClient, string path, CreateMode createMode)
        {
            return RecursionCreateAsync(zkClient, path, null, ZooDefs.Ids.OPEN_ACL_UNSAFE, createMode);
        }

        public static Task<string> RecursionCreateAsync(this ZooKeeper zkClient, string path, byte[] data, CreateMode createMode)
        {
            return RecursionCreateAsync(zkClient, path, data, ZooDefs.Ids.OPEN_ACL_UNSAFE, createMode);
        }

        public static async Task<string> RecursionCreateAsync(this ZooKeeper zkClient, string path, byte[] data, List<ACL> acl, CreateMode createMode)
        {
            var paths = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            //zkClient.
            string outPath = path;
            var currPath = string.Empty;
            for (int i = 0; i < paths.Length; i++)
            {
                currPath += "/" + paths[i];

                try
                {
                    if (i == paths.Length - 1) // 叶子节点
                    {
                        outPath = await zkClient.createAsync(currPath, data, acl, createMode);
                        return outPath;
                    }

                    var stat = await zkClient.existsAsync(currPath);
                    if (stat == null) // 节点不存在
                    {
                        await zkClient.createAsync(currPath, null, ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT);
                    }
                }
                catch (KeeperException.NodeExistsException)
                {
                    continue;
                }
            }
            return outPath;
        }

        public static async Task<string> GetDateStringAsync(this ZooKeeper zkClient, string path)
        {
            var dataResult = await zkClient.getDataAsync(path);

            return dataResult?.Data == null ? null : Encoding.UTF8.GetString(dataResult.Data);
        }

        public static async Task<int> GetDateIntAsync(this ZooKeeper zkClient, string path)
        {
            var dataResult = await zkClient.getDataAsync(path);

            string data = dataResult?.Data == null ? null : Encoding.UTF8.GetString(dataResult.Data);

            return int.Parse(data);
        }

    }
}
