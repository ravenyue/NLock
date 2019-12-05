using Microsoft.Extensions.DependencyInjection;
using System;

namespace NLock.Core
{
    public static class NLockServiceCollectionExtensions
    {
        public static IServiceCollection AddNLock(
            this IServiceCollection services,
            Action<NLockOptions> optionsAction)
        {
            var options = new NLockOptions();
            optionsAction(options);

            foreach (var serviceExtension in options.Extensions)
            {
                serviceExtension.AddServices(services);
            }
            services.Configure(optionsAction);

            return services;
        }
    }
}
