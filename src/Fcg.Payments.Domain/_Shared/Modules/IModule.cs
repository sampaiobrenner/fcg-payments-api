using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fcg.Payments.Domain._Shared.Modules;

public interface IModule
{
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
}
