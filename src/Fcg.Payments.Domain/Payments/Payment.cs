using System.Globalization;
using Fcg.Contracts.Events.V1;
using Fcg.Payments.Domain._Shared.Exceptions;
using Fcg.Payments.Domain._Shared.Models;
using Fcg.Payments.Domain.Properties;

namespace Fcg.Payments.Domain.Payments;

public sealed class Payment : DomainModelBase<PaymentModel>, IAggregateRoot
{
    public Payment(PaymentModel persistenceModel) : base(persistenceModel)
    {
    }

    public Guid OrderId => PersistenceModel.OrderId;
    public Guid UserId => PersistenceModel.UserId;
    public Guid GameId => PersistenceModel.GameId;
    public decimal Amount => PersistenceModel.Amount;
    public PaymentStatus Status => PersistenceModel.Status;
    public string? Reason => PersistenceModel.Reason;
    public DateTime ProcessedAt => PersistenceModel.ProcessedAt;

    /// <summary>
    /// Simula o processamento do pagamento: valores ate o limite de aprovacao (inclusive) sao aprovados;
    /// acima dele, rejeitados com o motivo preenchido.
    /// </summary>
    public static Payment Process(Guid orderId, Guid userId, Guid gameId, decimal amount, decimal approvalLimit)
    {
        if (amount < 0)
            throw new BusinessException(DomainResources.ValorDoPagamentoNegativo);

        var roundedAmount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        var approved = roundedAmount <= approvalLimit;

        return new Payment(new PaymentModel
        {
            OrderId = orderId,
            UserId = userId,
            GameId = gameId,
            Amount = roundedAmount,
            Status = approved ? PaymentStatus.Approved : PaymentStatus.Rejected,
            Reason = approved
                ? null
                : string.Format(CultureInfo.InvariantCulture, DomainResources.ValorExcedeLimiteDeAprovacao, roundedAmount, approvalLimit),
            ProcessedAt = DateTime.UtcNow
        });
    }
}
