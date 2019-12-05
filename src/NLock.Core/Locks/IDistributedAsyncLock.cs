using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NLock.Core.Locks
{
    public interface IDistributedAsyncLock : IDisposable
    {
        Task AcquireAsync();
        Task AcquireAsync(int millisecondsTimeout);
        Task AcquireAsync(TimeSpan timeout);

        //Task<bool> TryAcquireAsync();
        //Task<bool> TryAcquireAsync(int millisecondsTimeout);
        //Task<bool> TryAcquireAsync(TimeSpan timeout);

        Task ReleaseAsync();
    }
}
