using Fcg.Payments.Domain._Shared.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fcg.Payments.Domain;

public sealed class FcgPaymentsDomainModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
    }
}
