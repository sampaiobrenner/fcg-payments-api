using Fcg.Contracts.Events.V1;

namespace Fcg.Payments.Application.Payments.GetPaymentByOrderId;

public sealed record GetPaymentByOrderIdResponse(
    Guid PaymentId,
    Guid OrderId,
    Guid UserId,
    Guid GameId,
    decimal Amount,
    PaymentStatus Status,
    string? Reason,
    DateTime ProcessedAt);
