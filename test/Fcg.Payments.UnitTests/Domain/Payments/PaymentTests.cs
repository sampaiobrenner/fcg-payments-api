using Fcg.Contracts.Events.V1;
using Fcg.Payments.Domain._Shared.Exceptions;
using Fcg.Payments.Domain.Payments;

namespace Fcg.Payments.UnitTests.Domain.Payments;

public class PaymentTests
{
    private const decimal Limite = 300m;

    private static readonly Guid OrderId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly Guid GameId = Guid.CreateVersion7();

    [Fact]
    public void Process_DeveAprovarSemMotivo_QuandoValorIgualAoLimite()
    {
        var payment = Payment.Process(OrderId, UserId, GameId, 300.00m, Limite);

        payment.Status.Should().Be(PaymentStatus.Approved);
        payment.Reason.Should().BeNull();
        payment.Amount.Should().Be(300.00m);
    }

    [Fact]
    public void Process_DeveRejeitarComMotivo_QuandoValorAcimaDoLimite()
    {
        var payment = Payment.Process(OrderId, UserId, GameId, 300.01m, Limite);

        payment.Status.Should().Be(PaymentStatus.Rejected);
        payment.Reason.Should().Be("Valor 300.01 excede o limite de aprovação 300.00");
    }

    [Fact]
    public void Process_DeveLancarBusinessException_QuandoValorNegativo()
    {
        var act = () => Payment.Process(OrderId, UserId, GameId, -0.01m, Limite);

        act.Should().Throw<BusinessException>().WithMessage("O valor do pagamento não pode ser negativo.");
    }

    [Fact]
    public void Process_DeveAprovar_QuandoValorZero()
    {
        Payment.Process(OrderId, UserId, GameId, 0m, Limite).Status.Should().Be(PaymentStatus.Approved);
    }

    [Fact]
    public void Process_DeveUsarLimiteInformado_QuandoLimiteDiferenteDoPadrao()
    {
        var payment = Payment.Process(OrderId, UserId, GameId, 399.9m, 350m);

        payment.Status.Should().Be(PaymentStatus.Rejected);
        payment.Reason.Should().Be("Valor 399.90 excede o limite de aprovação 350.00");
    }

    [Fact]
    public void Process_DeveArredondarValorParaDuasCasas_QuandoValorComMaisCasas()
    {
        Payment.Process(OrderId, UserId, GameId, 10.005m, Limite).Amount.Should().Be(10.01m);
    }

    [Fact]
    public void Process_DevePreencherDadosDoPedido_QuandoProcessado()
    {
        var antes = DateTime.UtcNow;

        var payment = Payment.Process(OrderId, UserId, GameId, 199.90m, Limite);

        payment.Id().Should().NotBeEmpty();
        payment.OrderId.Should().Be(OrderId);
        payment.UserId.Should().Be(UserId);
        payment.GameId.Should().Be(GameId);
        payment.ProcessedAt.Kind.Should().Be(DateTimeKind.Utc);
        payment.ProcessedAt.Should().BeOnOrAfter(antes);
    }
}
