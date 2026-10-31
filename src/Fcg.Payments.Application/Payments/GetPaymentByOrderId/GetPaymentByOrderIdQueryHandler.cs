using Fcg.Payments.Application._Shared.Contexts;
using Fcg.Payments.Application._Shared.Exceptions;
using Fcg.Payments.Application._Shared.Parsers;
using Fcg.Payments.Application._Shared.Security;
using Fcg.Payments.Application.Properties;
using Fcg.Payments.Domain.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Payments.Application.Payments.GetPaymentByOrderId;

internal sealed class GetPaymentByOrderIdQueryHandler : IRequestHandler<GetPaymentByOrderIdQuery, GetPaymentByOrderIdResponse>
{
    private readonly IPaymentsDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IParser<PaymentModel, GetPaymentByOrderIdResponse> _parser;

    public GetPaymentByOrderIdQueryHandler(
        IPaymentsDbContext context,
        ICurrentUser currentUser,
        IParser<PaymentModel, GetPaymentByOrderIdResponse> parser)
    {
        _context = context;
        _currentUser = currentUser;
        _parser = parser;
    }

    public async Task<GetPaymentByOrderIdResponse> Handle(GetPaymentByOrderIdQuery request, CancellationToken cancellationToken)
    {
        var payment = await _context.DataSet<PaymentModel>()
            .FirstOrDefaultAsync(model => model.OrderId == request.OrderId, cancellationToken);

        // Quem nao e dono nem administrador recebe 404 (nao revela a existencia do pagamento).
        if (payment is null || (!_currentUser.IsAdministrator && payment.UserId != _currentUser.UserId))
            throw new NotFoundException(ApplicationResources.Pagamento, request.OrderId);

        return _parser.Parse(payment)!;
    }
}
