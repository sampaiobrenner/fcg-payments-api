namespace Fcg.Payments.Domain.Payments;

public interface IPaymentRepository
{
    Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task AddAsync(Payment payment, CancellationToken cancellationToken);
}
