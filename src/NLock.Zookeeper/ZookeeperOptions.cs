using org.apache.zookeeper;
using System;
using System.Collections.Generic;
using System.Text;

namespace NLock.Zookeeper
{
    public class ZookeeperOptions
    {
        public ZookeeperOptions()
        {
            SessionTimeout = 10000;
            DefaultLockTimeout = 10000;
        }

        public string ConnectionString { get; set; }

        public int SessionTimeout { get; set; }

        public Watcher Watcher { get; set; }

        public int DefaultLockTimeout { get; set; }
    }
}
