using Fcg.Payments.Domain._Shared.Exceptions;
using Fcg.Payments.Infrastructure._Shared.Context;
using FluentValidation;
using MassTransit;

namespace Fcg.Payments.WebApi._Shared.Messaging;

public static class MessagingServiceCollectionExtensions
{
    private static readonly TimeSpan[] RetryIntervals =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(30)
    ];

    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>() ?? new RabbitMqOptions();

        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();
            bus.AddConsumers(typeof(MessagingServiceCollectionExtensions).Assembly);

            bus.AddEntityFrameworkOutbox<PaymentsDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });

            bus.AddConfigureEndpointsCallback((context, _, endpoint) =>
                endpoint.UseEntityFrameworkOutbox<PaymentsDbContext>(context));

            bus.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.Host(options.Host, options.VirtualHost, host =>
                {
                    host.Username(options.Username);
                    host.Password(options.Password);
                });

                rabbit.UseMessageRetry(retry =>
                {
                    retry.Intervals(RetryIntervals);
                    retry.Ignore<BusinessException>();
                    retry.Ignore<ValidationException>();
                });

                rabbit.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
