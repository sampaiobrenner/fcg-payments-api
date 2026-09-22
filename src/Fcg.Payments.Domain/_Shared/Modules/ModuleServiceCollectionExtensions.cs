using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fcg.Payments.Domain._Shared.Modules;

public static class ModuleServiceCollectionExtensions
{
    public static IServiceCollection AddModule<TModule>(this IServiceCollection services, IConfiguration configuration)
        where TModule : IModule, new()
    {
        new TModule().ConfigureServices(services, configuration);
        return services;
    }
}
