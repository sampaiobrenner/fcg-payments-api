using Fcg.Contracts.Messaging;
using MassTransit;

namespace Fcg.Payments.WebApi.Payments.ProcessPayment;

public sealed class OrderPlacedConsumerDefinition : ConsumerDefinition<OrderPlacedConsumer>
{
    public const string QueueConfigurationKey = $"Messaging:Queues:{nameof(QueueNames.PaymentsOrderPlaced)}";

    public OrderPlacedConsumerDefinition(IConfiguration configuration)
    {
        var queueName = configuration[QueueConfigurationKey];
        EndpointName = string.IsNullOrWhiteSpace(queueName) ? QueueNames.PaymentsOrderPlaced : queueName;
    }
}
