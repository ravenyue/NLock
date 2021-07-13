using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NLock.Core.Locks
{
    public interface IDistributedAsyncLock : IDisposable
    {
        Task<bool> TryAcquireAsync(int millisecondsTimeout);
        Task ReleaseAsync();
    }
}
