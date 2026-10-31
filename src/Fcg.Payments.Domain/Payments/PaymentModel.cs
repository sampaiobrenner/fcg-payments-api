using Fcg.Contracts.Events.V1;
using Fcg.Payments.Domain._Shared.Models;

namespace Fcg.Payments.Domain.Payments;

public sealed class PaymentModel : PersistenceModelBase
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public Guid GameId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public string? Reason { get; set; }
    public DateTime ProcessedAt { get; set; }
}
