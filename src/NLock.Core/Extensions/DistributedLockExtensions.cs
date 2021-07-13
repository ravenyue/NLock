using NLock.Core.Locks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NLock.Core.Extensions
{
    public static class DistributedLockExtensions
    {
        #region Async method

        public static Task AcquireAsync(this IDistributedAsyncLock mutexLock)
        {
            return mutexLock.AcquireAsync(0);
        }

        public static Task AcquireAsync(this IDistributedAsyncLock mutexLock, TimeSpan timeout)
        {
            return mutexLock.AcquireAsync((int)timeout.TotalMilliseconds);
        }

        public static async Task AcquireAsync(this IDistributedAsyncLock mutexLock, int millisecondsTimeout)
        {
            var locked = await mutexLock.TryAcquireAsync(millisecondsTimeout);
            if (!locked)
            {
                throw new TimeoutException("Acquire lock timeout");
            }
        }

        public static Task TryAcquireAsync(this IDistributedAsyncLock mutexLock)
        {
            return mutexLock.TryAcquireAsync(0);
        }

        public static Task<bool> TryAcquireAsync(this IDistributedAsyncLock mutexLock, TimeSpan timeout)
        {
            return mutexLock.TryAcquireAsync((int)timeout.TotalMilliseconds);
        }

        #endregion


        #region Sync method

        public static void Acquire(this IDistributedLock mutexLock)
        {
            mutexLock.Acquire(0);
        }

        public static void Acquire(this IDistributedLock mutexLock, TimeSpan timeout)
        {
            mutexLock.Acquire((int)timeout.TotalMilliseconds);
        }

        public static void Acquire(this IDistributedLock mutexLock, int millisecondsTimeout)
        {
            var locked = mutexLock.TryAcquire(millisecondsTimeout);
            if (!locked)
            {
                throw new TimeoutException("Acquire lock timeout");
            }
        }

        public static void TryAcquire(this IDistributedLock mutexLock)
        {
            mutexLock.TryAcquire(0);
        }

        public static bool TryAcquire(this IDistributedLock mutexLock, TimeSpan timeout)
        {
            return mutexLock.TryAcquire((int)timeout.TotalMilliseconds);
        }

        #endregion
    }
}
