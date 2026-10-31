using Fcg.Contracts.Events.V1;
using Fcg.Contracts.Messaging;
using Fcg.Payments.Application.Payments.ProcessPayment;
using Fcg.Payments.WebApi.Payments.ProcessPayment;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Fcg.Payments.UnitTests.WebApi.Payments.ProcessPayment;

public class OrderPlacedConsumerTests
{
    [Fact]
    public async Task Consume_DeveEnviarProcessPaymentCommand_QuandoEventoRecebido()
    {
        var message = new OrderPlacedEvent(Guid.CreateVersion7(), DateTime.UtcNow, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), 199.90m);
        var context = new Mock<ConsumeContext<OrderPlacedEvent>>();
        context.Setup(c => c.Message).Returns(message);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        var sender = new Mock<ISender>();

        await new OrderPlacedConsumer(sender.Object).Consume(context.Object);

        sender.Verify(s => s.Send(
            new ProcessPaymentCommand(message.OrderId, message.UserId, message.GameId, message.Price),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Definition_DeveUsarFilaDoContrato_QuandoNaoConfigurada()
    {
        EndpointNameOf(new OrderPlacedConsumerDefinition(new ConfigurationBuilder().Build()))
            .Should().Be(QueueNames.PaymentsOrderPlaced);
    }

    [Fact]
    public void Definition_DeveUsarFilaDaConfiguracao_QuandoMessagingQueuesInformado()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Messaging:Queues:PaymentsOrderPlaced"] = "outra-fila" })
            .Build();

        EndpointNameOf(new OrderPlacedConsumerDefinition(configuration)).Should().Be("outra-fila");
    }

    private static string EndpointNameOf(IConsumerDefinition definition)
        => definition.GetEndpointName(KebabCaseEndpointNameFormatter.Instance);
}
