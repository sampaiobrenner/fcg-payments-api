using Fcg.Payments.Application._Shared.Contexts;
using Fcg.Payments.Application._Shared.Messaging;
using Fcg.Payments.Domain._Shared.Modules;
using Fcg.Payments.Infrastructure._Shared.Context;
using Fcg.Payments.Infrastructure._Shared.Messaging;
using Fcg.Payments.Infrastructure.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Fcg.Payments.Infrastructure;

public sealed class FcgPaymentsInfrastructureModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(InfrastructureResources.ConnectionStringAusente);

        var npgsqlConnectionString = new NpgsqlConnectionStringBuilder(connectionString)
        {
            GssEncryptionMode = GssEncryptionMode.Disable
        }.ConnectionString;

        services.AddDbContext<PaymentsDbContext>(options => options
            .UseNpgsql(npgsqlConnectionString)
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IPaymentsDbContext>(provider => provider.GetRequiredService<PaymentsDbContext>());
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
    }
}
