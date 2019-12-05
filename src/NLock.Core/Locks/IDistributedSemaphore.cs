using System;
using System.Collections.Generic;
using System.Text;

namespace NLock.Core.Locks
{
    /// <summary>
    /// 分布式信号量
    /// </summary>
    public interface IDistributedSemaphore : IDistributedAsyncSemaphore, IDistributedLock, IDisposable
    {
        int AcquiredCount { get; }

        void AcquireMultiple(int quantity);
        void AcquireMultiple(int quantity, int millisecondsTimeout);
        void AcquireMultiple(int quantity, TimeSpan timeout);

        bool TryAcquireMultiple(int quantity);
        bool TryAcquireMultiple(int quantity, int millisecondsTimeout);
        bool TryAcquireMultiple(int quantity, TimeSpan timeout);

        void Release(int quantity);
        void ReleaseAll();
    }
}
