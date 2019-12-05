using System;
using System.Collections.Generic;
using System.Text;

namespace NLock.Zookeeper
{
    public interface ILockInternalsSorter
    {
        string FixForSorting(string str, string lockName);
    }
}
