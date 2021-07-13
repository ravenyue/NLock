using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace NLock.StackExchangeRedis.Utils
{
    public static class TimeoutHelper
    {
        /// <summary>
        /// Returns <see cref="Environment.TickCount"/> as a start time in milliseconds as a <see cref="uint"/>.
        /// <see cref="Environment.TickCount"/> rolls over from positive to negative every ~25 days, then ~25 days to back to positive again.
        /// <see cref="uint"/> is used to ignore the sign and double the range to 50 days.
        /// </summary>
        public static uint GetTime()
        {
            return (uint)Environment.TickCount;
        }

        public static int UpdateTimeOut(uint startTime, int originalWaitMillisecondsTimeout)
        {
            uint elapsedMilliseconds = (GetTime() - startTime);

            // Check the elapsed milliseconds is greater than max int because this property is uint
            if (elapsedMilliseconds > int.MaxValue)
            {
                return 0;
            }

            // Subtract the elapsed time from the current wait time
            int currentWaitTimeout = originalWaitMillisecondsTimeout - (int)elapsedMilliseconds;
            if (currentWaitTimeout <= 0)
            {
                return 0;
            }

            return currentWaitTimeout;
        }

        public static bool IsTimeout(uint startTime, int originalWaitMillisecondsTimeout)
        {
            if (originalWaitMillisecondsTimeout == Timeout.Infinite)
            {
                return false;
            }

            return UpdateTimeOut(startTime, originalWaitMillisecondsTimeout) <= 0;
        }
    }
}
