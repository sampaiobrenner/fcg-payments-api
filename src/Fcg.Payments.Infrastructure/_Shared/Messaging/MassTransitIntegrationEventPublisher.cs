using Fcg.Payments.Application._Shared.Messaging;
using MassTransit;

namespace Fcg.Payments.Infrastructure._Shared.Messaging;

internal sealed class MassTransitIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitIntegrationEventPublisher(IPublishEndpoint publishEndpoint) => _publishEndpoint = publishEndpoint;

    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class
        => _publishEndpoint.Publish(integrationEvent, cancellationToken);
}
