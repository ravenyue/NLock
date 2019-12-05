using Microsoft.Extensions.DependencyInjection;

namespace NLock.Core
{
    public interface INLockOptionsExtension
    {
        void AddServices(IServiceCollection services);
    }
}