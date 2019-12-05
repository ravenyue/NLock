using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NLock.Core.Locks
{
    public interface IDistributedAsyncSemaphore
    {
        Task AcquireMultipleAsync(int quantity);
        Task AcquireMultipleAsync(int quantity, int millisecondsTimeout);
        Task AcquireMultipleAsync(int quantity, TimeSpan timeout);

        //Task<bool> TryAcquireMultipleAsync(int quantity);
        //Task<bool> TryAcquireMultipleAsync(int quantity, int millisecondsTimeout);
        //Task<bool> TryAcquireMultipleAsync(int quantity, TimeSpan timeout);

        Task ReleaseAsync(int quantity);
        Task ReleaseAllAsync();
    }
}
