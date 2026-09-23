using System.Text.Json.Serialization;
using Fcg.Payments.Application._Shared.Security;
using Fcg.Payments.Domain._Shared.Modules;
using Fcg.Payments.Infrastructure._Shared.Context;
using Fcg.Payments.WebApi._Shared.Endpoints;
using Fcg.Payments.WebApi._Shared.Errors;
using Fcg.Payments.WebApi._Shared.HealthChecks;
using Fcg.Payments.WebApi._Shared.Messaging;
using Fcg.Payments.WebApi._Shared.Security;

namespace Fcg.Payments.WebApi;

public sealed class FcgPaymentsWebApiModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddOpenApi();
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        services.AddJwtAuthentication(configuration);
        services.AddMessaging(configuration);

        services.AddHealthChecks()
            .AddDbContextCheck<PaymentsDbContext>("postgres", tags: [HealthCheckTags.Ready]);

        services.AddEndpoints(typeof(FcgPaymentsWebApiModule).Assembly);
    }
}
