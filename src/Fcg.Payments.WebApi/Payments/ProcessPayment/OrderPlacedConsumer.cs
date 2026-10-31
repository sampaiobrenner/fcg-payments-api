using Fcg.Contracts.Events.V1;
using Fcg.Payments.Application.Payments.ProcessPayment;
using MassTransit;
using MediatR;

namespace Fcg.Payments.WebApi.Payments.ProcessPayment;

public sealed class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
{
    private readonly ISender _sender;

    public OrderPlacedConsumer(ISender sender) => _sender = sender;

    public Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        var message = context.Message;
        return _sender.Send(new ProcessPaymentCommand(message.OrderId, message.UserId, message.GameId, message.Price), context.CancellationToken);
    }
}
