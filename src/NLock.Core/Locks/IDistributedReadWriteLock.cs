using System;
using System.Collections.Generic;
using System.Text;

namespace NLock.Core.Locks
{
    /// <summary>
    /// 分布式读写锁
    /// </summary>
    public interface IDistributedReadWriteLock
    {
        IDistributedLock ReadLock();

        IDistributedLock WriteLock();
    }
}
