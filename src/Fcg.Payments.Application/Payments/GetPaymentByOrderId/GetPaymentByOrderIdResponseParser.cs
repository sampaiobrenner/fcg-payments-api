using Fcg.Payments.Application._Shared.Parsers;
using Fcg.Payments.Domain.Payments;

namespace Fcg.Payments.Application.Payments.GetPaymentByOrderId;

internal sealed class GetPaymentByOrderIdResponseParser : Parser<PaymentModel, GetPaymentByOrderIdResponse>
{
    protected override GetPaymentByOrderIdResponse DoParse(PaymentModel source)
        => new(
            source.Id,
            source.OrderId,
            source.UserId,
            source.GameId,
            source.Amount,
            source.Status,
            source.Reason,
            source.ProcessedAt);
}
