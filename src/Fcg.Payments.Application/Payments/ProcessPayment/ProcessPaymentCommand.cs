using MediatR;

namespace Fcg.Payments.Application.Payments.ProcessPayment;

public sealed record ProcessPaymentCommand(Guid OrderId, Guid UserId, Guid GameId, decimal Price) : IRequest;
