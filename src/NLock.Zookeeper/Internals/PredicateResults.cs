namespace NLock.Zookeeper
{
    public class PredicateResults
    {
        public PredicateResults(string nodeToWatch, bool locked)
        {
            NodeToWatch = nodeToWatch;
            Locked = locked;
        }

        /// <summary>
        /// 要监听的节点名
        /// </summary>
        public string NodeToWatch { get; }

        /// <summary>
        /// 是否已获取到锁
        /// </summary>
        public bool Locked { get; }
    }
}