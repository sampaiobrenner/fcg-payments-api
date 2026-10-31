using MediatR;

namespace Fcg.Payments.Application.Payments.GetPaymentByOrderId;

public sealed record GetPaymentByOrderIdQuery(Guid OrderId) : IRequest<GetPaymentByOrderIdResponse>;
