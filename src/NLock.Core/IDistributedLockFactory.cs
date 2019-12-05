using NLock.Core.Locks;
using System;
using System.Collections.Generic;
using System.Text;

namespace NLock.Core
{
    public interface IDistributedLockFactory
    {
        IDistributedLock CreateMutexLock(string name);
        IDistributedReadWriteLock CreateReadWriteLock(string name);
        IDistributedSemaphore CreateSemaphore(string name, int maxLeases);
    }
}
