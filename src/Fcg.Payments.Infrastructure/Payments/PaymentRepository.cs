using Fcg.Payments.Domain.Payments;
using Fcg.Payments.Infrastructure._Shared.Context;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Payments.Infrastructure.Payments;

internal sealed class PaymentRepository : IPaymentRepository
{
    private readonly PaymentsDbContext _context;

    public PaymentRepository(PaymentsDbContext context) => _context = context;

    public async Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var model = await _context.Set<PaymentModel>()
            .FirstOrDefaultAsync(payment => payment.OrderId == orderId, cancellationToken);

        return model is null ? null : new Payment(model);
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken)
        => await _context.Set<PaymentModel>().AddAsync(payment.PersistenceModel, cancellationToken);
}
