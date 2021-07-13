using System;
using System.Collections.Generic;
using System.Text;

namespace NLock.Core.Locks
{
    public interface IDistributedLock : IDistributedAsyncLock, IDisposable
    {
        //void Acquire();
        //void Acquire(int millisecondsTimeout);
        //void Acquire(TimeSpan timeout);

        //bool TryAcquire();
        bool TryAcquire(int millisecondsTimeout);
        //bool TryAcquire(TimeSpan timeout);

        void Release();
    }
}
