using org.apache.zookeeper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NLock.Zookeeper
{
    public class ReleaseLockWatcher : Watcher
    {
        private readonly SemaphoreSlim _signal;

        public ReleaseLockWatcher(SemaphoreSlim signal)
        {
            _signal = signal;
        }

        public override Task process(WatchedEvent @event)
        {
            _signal.Release();
            return Task.CompletedTask;
        }
    }
}
