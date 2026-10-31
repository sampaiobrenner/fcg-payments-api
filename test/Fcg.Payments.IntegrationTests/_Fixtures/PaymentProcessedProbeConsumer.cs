using Fcg.Contracts.Events.V1;
using MassTransit;

namespace Fcg.Payments.IntegrationTests._Fixtures;

/// <summary>
/// Assina o <see cref="PaymentProcessedEvent"/> como fariam Catalog e Notifications. Necessario porque o EF Outbox
/// entrega o corpo ja serializado, entao o harness so enxerga o evento tipado (e o JSON real) do lado do consumo.
/// </summary>
public sealed class PaymentProcessedProbeConsumer : IConsumer<PaymentProcessedEvent>
{
    public Task Consume(ConsumeContext<PaymentProcessedEvent> context) => Task.CompletedTask;
}
