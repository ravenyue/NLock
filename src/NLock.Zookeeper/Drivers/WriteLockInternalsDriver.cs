using System;
using System.Collections.Generic;
using System.Text;

namespace NLock.Zookeeper
{
    public class WriteLockInternalsDriver : StandardLockInternalsDriver
    {
        public override string FixForSorting(string str, string lockName)
        {
            str = base.FixForSorting(str, ZookeeperReadWriteLock.READ_LOCK_NAME);
            str = base.FixForSorting(str, ZookeeperReadWriteLock.WRITE_LOCK_NAME);

            return str;
        }
    }
}
