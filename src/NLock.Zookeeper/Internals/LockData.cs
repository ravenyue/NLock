using System.Threading;

namespace NLock.Zookeeper
{
    public class LockData
    {
        public string LockPath { get; }
        // 确保原子操作
        private int lockCount = 1;

        public LockData(string lockPath)
        {
            LockPath = lockPath;
        }

        /// <summary>
        /// 加锁次数自增
        /// </summary>
        /// <returns></returns>
        public int CountIncrement()
        {
            return ++lockCount;
        }

        /// <summary>
        /// 加锁次数自减
        /// </summary>
        /// <returns></returns>
        public int CountDecrement()
        {
            return --lockCount;
        }
    }
}