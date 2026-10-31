using Fcg.Payments.Application._Shared.Behaviors;
using Fcg.Payments.Application._Shared.Parsers;
using Fcg.Payments.Application.Payments._Shared;
using Fcg.Payments.Domain._Shared.Modules;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fcg.Payments.Application;

public sealed class FcgPaymentsApplicationModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var assembly = typeof(FcgPaymentsApplicationModule).Assembly;

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.Configure<PaymentsOptions>(configuration.GetSection(PaymentsOptions.SectionName));

        AddParsers(services);
    }

    private static void AddParsers(IServiceCollection services)
    {
        var parserTypes = typeof(FcgPaymentsApplicationModule).Assembly.DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false });

        foreach (var parserType in parserTypes)
        {
            var contracts = parserType.ImplementedInterfaces
                .Where(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IParser<,>));

            foreach (var contract in contracts)
                services.AddSingleton(contract, parserType);
        }
    }
}
