using Microsoft.Extensions.DependencyInjection;
using NLock.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace NLock.Zookeeper
{
    public class ZookeeperLockOptionsExtension : INLockOptionsExtension
    {
        private readonly Action<ZookeeperOptions> _optionsAction;

        public ZookeeperLockOptionsExtension(Action<ZookeeperOptions> optionsAction)
        {
            _optionsAction = optionsAction;
        }

        public void AddServices(IServiceCollection services)
        {
            services.Configure(_optionsAction);
            services.AddScoped<IDistributedLockFactory, ZookeeperLockFactory>();
        }
    }
}
