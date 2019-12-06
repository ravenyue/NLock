using NLock.Core;
using org.apache.zookeeper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NLock.Zookeeper
{
    public class LockInternals : IDisposable
    {
        private readonly ILockInternalsDriver _driver;
        private readonly string _lockName;
        private readonly int _maxLeases;
        private readonly ZooKeeper _zkClient;
        private readonly string _basePath;
        private readonly string _path;
        private readonly Watcher _watcher;
        private readonly SemaphoreSlim _signal;

        public LockInternals(ZooKeeper zkClient, ILockInternalsDriver driver, string path, string lockName, int maxLeases)
        {
            _driver = driver;
            _lockName = lockName;
            _maxLeases = maxLeases;
            _zkClient = zkClient;
            _basePath = ZKPaths.ValidatePath(path);
            _path = ZKPaths.MakePath(path, lockName);
            _signal = new SemaphoreSlim(0);
            _watcher = new ReleaseLockWatcher(_signal);
        }

        /// <summary>
        /// 获取锁
        /// </summary>
        /// <param name="timeout">超时时间(毫秒)</param>
        /// <returns>成功获取锁返回锁在ZooKeeper中的path,否则返回null</returns>
        public async Task<string> AttemptLockAsync(int timeout)
        {
            // 开始时间戳
            var startMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // 等待毫秒数
            var millisToWait = timeout;
            // 锁的路径
            string ourPath;
            // 是否成功获取锁
            var hasTheLock = false;

            // 创建锁的临时有序节点
            ourPath = await _driver.CreateLockNodeAsync(_zkClient, _path);
            // 判断自身是否能够持有锁。如果不能，进入wait，直到超时。
            hasTheLock = await InternalLockLoopAsync(startMillis, millisToWait, ourPath);

            if (hasTheLock)
            {
                return ourPath;
            }

            return null;
        }

        public Task ReleaseLockAsync(string lockPath)
        {
            return DeleteOurPathAsync(lockPath);
        }

        private async Task<bool> InternalLockLoopAsync(long startMillis, int millisToWait, string ourPath)
        {
            var haveTheLock = false;
            var doDelete = false;

            try
            {
                while (_zkClient.getState() == ZooKeeper.States.CONNECTED)
                {
                    // 获取排序的子节点
                    var children = await GetSortedChildren();
                    //var sequenceNodeName = ourPath.Substring(_basePath.Length + 1);

                    // 判断能否持有锁
                    var predicateResults = _driver.GetTheLock(_zkClient, children, ourPath, _maxLeases);

                    // 已获取到锁
                    if (predicateResults.Locked)
                    {
                        return true;
                    }

                    // 上一个节点路径
                    var previousSequencePath = ZKPaths.MakePath(_basePath, predicateResults.NodeToWatch);

                    try
                    {
                        // 剩余等待时间
                        millisToWait -= (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - startMillis);
                        startMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        if (millisToWait <= 0)
                        {
                            // 超时，删除创建的节点
                            doDelete = true;
                            break;
                        }

                        // 设置释放锁监听
                        await _zkClient.getDataAsync(previousSequencePath, _watcher);
                        // 阻塞 等待锁释放或超时
                        if (!await _signal.WaitAsync(millisToWait))
                        {
                            doDelete = true;
                            break;
                        }
                    }
                    catch (KeeperException.NoNodeException ex)
                    {
                        // 上一个节点已被删除（即释放锁）。再次尝试获取
                    }

                }
            }
            catch (Exception ex)
            {
                doDelete = true;
                throw ex;
            }
            finally
            {
                if (doDelete)
                {
                    await DeleteOurPathAsync(ourPath);
                }
            }

            return haveTheLock;

        }

        private async Task DeleteOurPathAsync(string ourPath)
        {
            try
            {
                await _zkClient.deleteAsync(ourPath);
            }
            catch (KeeperException.NoNodeException e)
            {
                // 忽略-已删除（可能已过期会话等）
            }
        }

        public Task<List<string>> GetSortedChildren()
        {
            return GetSortedChildren(_lockName, _driver);
        }

        public async Task<List<string>> GetSortedChildren(string lockName, ILockInternalsSorter sorter)
        {
            var children = await _zkClient.getChildrenAsync(_basePath);

            children.Children.Sort((x, y) =>
            {
                return sorter.FixForSorting(x, lockName)
                    .CompareTo(sorter.FixForSorting(y, lockName));
            });

            return children.Children;
        }

        public void Dispose()
        {
            _signal.Dispose();
            try
            {
                var result = _zkClient.getChildrenAsync(_basePath).GetAwaiter().GetResult();
                if (result.Stat.getNumChildren() == 0)
                {
                    // 尝试删除锁节点以重置序列号
                    _zkClient.deleteAsync(_basePath).GetAwaiter().GetResult();
                }
            }
            catch (KeeperException.BadVersionException)
            {
                // 忽略-另一个线程/进程获得了锁
            }
            catch (KeeperException.NotEmptyException)
            {
                // 忽略-其他线程/进程正在等待
            }

        }
    }
}
