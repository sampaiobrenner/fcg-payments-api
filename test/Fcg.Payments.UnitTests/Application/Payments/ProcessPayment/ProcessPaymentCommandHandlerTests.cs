using Fcg.Contracts.Events.V1;
using Fcg.Payments.Application._Shared.Contexts;
using Fcg.Payments.Application._Shared.Messaging;
using Fcg.Payments.Application.Payments._Shared;
using Fcg.Payments.Application.Payments.ProcessPayment;
using Fcg.Payments.Domain._Shared.Exceptions;
using Fcg.Payments.Domain.Payments;
using Microsoft.Extensions.Options;
using Moq;

namespace Fcg.Payments.UnitTests.Application.Payments.ProcessPayment;

public class ProcessPaymentCommandHandlerTests
{
    private readonly Mock<IPaymentRepository> _repository = new();
    private readonly Mock<IPaymentsDbContext> _context = new();
    private readonly Mock<IIntegrationEventPublisher> _publisher = new();
    private readonly List<string> _calls = [];
    private readonly List<PaymentProcessedEvent> _published = [];
    private readonly ProcessPaymentCommandHandler _handler;

    public ProcessPaymentCommandHandlerTests()
    {
        _repository
            .Setup(repository => repository.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Callback(() => _calls.Add("add"))
            .Returns(Task.CompletedTask);

        _publisher
            .Setup(publisher => publisher.PublishAsync(It.IsAny<PaymentProcessedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PaymentProcessedEvent, CancellationToken>((integrationEvent, _) =>
            {
                _calls.Add("publish");
                _published.Add(integrationEvent);
            })
            .Returns(Task.CompletedTask);

        _context
            .Setup(context => context.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => _calls.Add("save"))
            .ReturnsAsync(1);

        _handler = new ProcessPaymentCommandHandler(
            _repository.Object,
            _context.Object,
            _publisher.Object,
            new PaymentProcessedEventParser(),
            Options.Create(new PaymentsOptions()));
    }

    [Fact]
    public async Task Handle_DeveAprovarPersistirEPublicar_QuandoPrecoDentroDoLimite()
    {
        var command = NewCommand(199.90m);

        await _handler.Handle(command, CancellationToken.None);

        _repository.Verify(repository => repository.AddAsync(
            It.Is<Payment>(payment => payment.OrderId == command.OrderId && payment.Status == PaymentStatus.Approved),
            It.IsAny<CancellationToken>()), Times.Once);

        var integrationEvent = _published.Should().ContainSingle().Subject;
        integrationEvent.OrderId.Should().Be(command.OrderId);
        integrationEvent.UserId.Should().Be(command.UserId);
        integrationEvent.GameId.Should().Be(command.GameId);
        integrationEvent.Price.Should().Be(199.90m);
        integrationEvent.Status.Should().Be(PaymentStatus.Approved);
        integrationEvent.Reason.Should().BeNull();
        integrationEvent.EventId.Should().NotBeEmpty();

        _calls.Should().Equal("add", "publish", "save");
    }

    [Fact]
    public async Task Handle_DeveRejeitarComMotivo_QuandoPrecoAcimaDoLimite()
    {
        await _handler.Handle(NewCommand(399.90m), CancellationToken.None);

        var integrationEvent = _published.Should().ContainSingle().Subject;
        integrationEvent.Status.Should().Be(PaymentStatus.Rejected);
        integrationEvent.Reason.Should().Be("Valor 399.90 excede o limite de aprovação 300.00");
        _calls.Should().Equal("add", "publish", "save");
    }

    [Fact]
    public async Task Handle_DeveUsarLimiteDasOpcoes_QuandoLimiteConfigurado()
    {
        var handler = new ProcessPaymentCommandHandler(
            _repository.Object, _context.Object, _publisher.Object, new PaymentProcessedEventParser(),
            Options.Create(new PaymentsOptions { ApprovalLimit = 500m }));

        await handler.Handle(NewCommand(399.90m), CancellationToken.None);

        _published.Should().ContainSingle().Which.Status.Should().Be(PaymentStatus.Approved);
    }

    [Fact]
    public async Task Handle_DeveRepublicarSemReprocessar_QuandoPagamentoJaExiste()
    {
        var command = NewCommand(999m);
        var existing = Payment.Process(command.OrderId, command.UserId, command.GameId, 199.90m, 300m);
        _repository
            .Setup(repository => repository.GetByOrderIdAsync(command.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await _handler.Handle(command, CancellationToken.None);
        await _handler.Handle(command, CancellationToken.None);

        _repository.Verify(repository => repository.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
        _published.Should().HaveCount(2);
        _published.Should().AllSatisfy(integrationEvent =>
        {
            integrationEvent.Status.Should().Be(PaymentStatus.Approved);
            integrationEvent.Price.Should().Be(199.90m);
        });
        _published.Select(integrationEvent => integrationEvent.EventId).Should().OnlyHaveUniqueItems();
        _calls.Should().Equal("publish", "save", "publish", "save");
    }

    [Fact]
    public async Task Handle_DeveLancarBusinessExceptionSemPublicar_QuandoPrecoNegativo()
    {
        var act = () => _handler.Handle(NewCommand(-1m), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>();
        _calls.Should().BeEmpty();
    }

    private static ProcessPaymentCommand NewCommand(decimal price)
        => new(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), price);
}
