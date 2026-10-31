using Fcg.Contracts.Events.V1;
using Fcg.Payments.Application._Shared.Parsers;
using Fcg.Payments.Domain.Payments;

namespace Fcg.Payments.Application.Payments.ProcessPayment;

/// <summary>Gera o evento de integracao a partir do pagamento; cada chamada produz um novo <c>EventId</c>.</summary>
internal sealed class PaymentProcessedEventParser : Parser<Payment, PaymentProcessedEvent>
{
    protected override PaymentProcessedEvent DoParse(Payment source)
        => new(
            Guid.CreateVersion7(),
            DateTime.UtcNow,
            source.OrderId,
            source.UserId,
            source.GameId,
            Price: source.Amount,
            source.Status,
            source.Reason);
}
