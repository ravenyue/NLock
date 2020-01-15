using NLock.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace NLock.Zookeeper
{
    public static class NLockOptionsExtension
    {
        public static NLockOptions UseZookeeper(this NLockOptions options, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            return UseZookeeper(options, config => config.ConnectionString = connectionString);
        }

        public static NLockOptions UseZookeeper(this NLockOptions options, string connectionString, int defaultLockTimeout)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            return UseZookeeper(options, config =>
            {
                config.ConnectionString = connectionString;
                config.DefaultLockTimeout = defaultLockTimeout;
            });
        }

        public static NLockOptions UseZookeeper(this NLockOptions options, Action<ZookeeperOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }
            options.RegisterExtension(new ZookeeperLockOptionsExtension(configure));

            return options;
        }
    }
}
