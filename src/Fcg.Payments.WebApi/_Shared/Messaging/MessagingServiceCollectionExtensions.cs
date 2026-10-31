using System.Text.Json;
using System.Text.Json.Serialization;
using Fcg.Payments.Domain._Shared.Exceptions;
using Fcg.Payments.Infrastructure._Shared.Context;
using FluentValidation;
using MassTransit;
using MassTransit.Serialization;

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

        // Payload conforme o contrato (fcg-contracts/samples): enums como string ("status": "Approved") e decimais como
        // numero ("price": 199.90). O padrao do MassTransit escreve enum como numero e decimal como texto. As opcoes do
        // serializer sao globais; configuradas aqui valem para qualquer transporte (RabbitMQ ou harness em memoria).
        SystemTextJsonMessageSerializer.Options = WithContractConventions(SystemTextJsonMessageSerializer.Options);

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

    private static JsonSerializerOptions WithContractConventions(JsonSerializerOptions options)
    {
        if (options.Converters.Any(converter => converter is DecimalNumberJsonConverter))
            return options;

        var configured = new JsonSerializerOptions(options);

        foreach (var decimalConverter in configured.Converters.Where(converter => converter is JsonConverter<decimal>).ToList())
            configured.Converters.Remove(decimalConverter);

        configured.Converters.Insert(0, new DecimalNumberJsonConverter());
        configured.Converters.Add(new JsonStringEnumConverter());
        return configured;
    }
}
