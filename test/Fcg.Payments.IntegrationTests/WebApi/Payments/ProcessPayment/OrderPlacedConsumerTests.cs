using System.Text.Json;
using System.Text.Json.Nodes;
using Fcg.Contracts.Events.V1;
using Fcg.Contracts.Messaging;
using Fcg.Payments.Domain.Payments;
using Fcg.Payments.Infrastructure._Shared.Context;
using Fcg.Payments.IntegrationTests._Fixtures;
using Fcg.Payments.WebApi.Payments.ProcessPayment;
using MassTransit;
using MassTransit.Serialization;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fcg.Payments.IntegrationTests.WebApi.Payments.ProcessPayment;

[Collection(PaymentsApiCollection.Name)]
public class OrderPlacedConsumerTests
{
    private readonly PaymentsApiFixture _fixture;
    private readonly ITestHarness _harness;

    public OrderPlacedConsumerTests(PaymentsApiFixture fixture)
    {
        _fixture = fixture;
        _harness = fixture.Factory.Services.GetTestHarness();
    }

    [Fact]
    public async Task Consume_DeveAprovarEPublicarEvento_QuandoPrecoDentroDoLimite()
    {
        var orderPlaced = NewOrderPlaced(199.90m);

        await _harness.Bus.Publish(orderPlaced);

        var published = await _harness.WaitForPublishedEventsAsync<PaymentProcessedEvent>(e => e.OrderId == orderPlaced.OrderId);
        var paymentEvent = published.Should().ContainSingle().Subject;
        paymentEvent.Status.Should().Be(PaymentStatus.Approved);
        paymentEvent.Reason.Should().BeNull();
        paymentEvent.Price.Should().Be(199.90m);
        paymentEvent.UserId.Should().Be(orderPlaced.UserId);
        paymentEvent.GameId.Should().Be(orderPlaced.GameId);

        var payments = await PaymentsOf(orderPlaced.OrderId);
        var payment = payments.Should().ContainSingle().Subject;
        payment.Status.Should().Be(PaymentStatus.Approved);
        payment.Amount.Should().Be(199.90m);
        payment.Reason.Should().BeNull();
    }

    [Fact]
    public async Task Consume_DeveRejeitarComMotivo_QuandoPrecoAcimaDoLimite()
    {
        var orderPlaced = NewOrderPlaced(399.90m);

        await _harness.Bus.Publish(orderPlaced);

        var published = await _harness.WaitForPublishedEventsAsync<PaymentProcessedEvent>(e => e.OrderId == orderPlaced.OrderId);
        var paymentEvent = published.Should().ContainSingle().Subject;
        paymentEvent.Status.Should().Be(PaymentStatus.Rejected);
        paymentEvent.Reason.Should().Be("Valor 399.90 excede o limite de aprovação 300.00");

        var payment = (await PaymentsOf(orderPlaced.OrderId)).Should().ContainSingle().Subject;
        payment.Status.Should().Be(PaymentStatus.Rejected);
        payment.Reason.Should().Be(paymentEvent.Reason);
    }

    [Fact]
    public async Task Consume_DeveRepublicarResultadoSemReprocessar_QuandoPedidoDuplicado()
    {
        var orderPlaced = NewOrderPlaced(199.90m);

        await _harness.Bus.Publish(orderPlaced);
        await _harness.WaitForPublishedEventsAsync<PaymentProcessedEvent>(e => e.OrderId == orderPlaced.OrderId);

        // Mesmo pedido, nova mensagem (outro MessageId e preco diferente): nao pode reprocessar.
        await _harness.Bus.Publish(orderPlaced with { EventId = Guid.CreateVersion7(), Price = 999m });

        var published = await _harness.WaitForPublishedEventsAsync<PaymentProcessedEvent>(e => e.OrderId == orderPlaced.OrderId, expectedCount: 2);
        published.Should().HaveCount(2);
        published.Select(e => e.EventId).Should().OnlyHaveUniqueItems();
        published.Should().AllSatisfy(e =>
        {
            e.Status.Should().Be(PaymentStatus.Approved);
            e.Price.Should().Be(199.90m);
        });

        (await PaymentsOf(orderPlaced.OrderId)).Should().ContainSingle();
    }

    [Fact]
    public async Task Consume_DevePublicarPayloadDoContrato_QuandoProcessarSampleDoOrderPlaced()
    {
        var orderPlaced = JsonSerializer.Deserialize<OrderPlacedEvent>(ReadSample("order-placed.json"), SystemTextJsonMessageSerializer.Options)!;

        await _harness.Bus.Publish(orderPlaced);

        var received = (await _harness.WaitForConsumedAsync<PaymentProcessedEvent>(e => e.OrderId == orderPlaced.OrderId))
            .Should().ContainSingle().Subject;

        // JSON exatamente como trafegou no broker (envelope MassTransit).
        var envelope = JsonNode.Parse(received.Context.ReceiveContext.Body.GetString())!.AsObject();
        envelope["messageType"]!.AsArray().Select(type => type!.GetValue<string>())
            .Should().Contain("urn:message:fcg:payment-processed:v1");

        var expected = JsonNode.Parse(ReadSample("payment-processed.json"))!.AsObject();
        var actual = envelope["message"]!.AsObject();

        actual.Select(property => property.Key).Should().BeEquivalentTo(expected.Select(property => property.Key));
        actual["status"]!.GetValueKind().Should().Be(JsonValueKind.String);
        actual["status"]!.GetValue<string>().Should().Be(expected["status"]!.GetValue<string>());
        actual["reason"].Should().BeNull();
        actual["price"]!.GetValueKind().Should().Be(JsonValueKind.Number);
        actual["price"]!.ToJsonString().Should().Be(expected["price"]!.ToJsonString());

        foreach (var key in new[] { "orderId", "userId", "gameId" })
            actual[key]!.GetValue<Guid>().Should().Be(expected[key]!.GetValue<Guid>(), key);

        actual["eventId"]!.GetValue<Guid>().Should().NotBe(orderPlaced.EventId);
        actual["occurredAt"]!.GetValue<DateTime>().Should().BeAfter(orderPlaced.OccurredAt);
    }

    [Fact]
    public async Task Consume_DeveReceberNaFilaDoContrato_QuandoEventoPublicado()
    {
        var orderPlaced = NewOrderPlaced(10m);

        await _harness.Bus.Publish(orderPlaced);
        await _harness.WaitForPublishedEventsAsync<PaymentProcessedEvent>(e => e.OrderId == orderPlaced.OrderId);

        var consumed = _harness.Consumed.Select<OrderPlacedEvent>(message => message.Context.Message.OrderId == orderPlaced.OrderId).First();
        consumed.Context.ReceiveContext.InputAddress.AbsolutePath.Trim('/').Should().Be(QueueNames.PaymentsOrderPlaced);
    }

    [Fact]
    public async Task Consume_DeveUsarLimiteDaConfiguracao_QuandoApprovalLimitSobrescrito()
    {
        await using var factory = _fixture.CreateFactory(new Dictionary<string, string?>
        {
            ["Payments:ApprovalLimit"] = "500",
            [OrderPlacedConsumerDefinition.QueueConfigurationKey] = "payments.order-placed.limite-500"
        });
        var harness = factory.Services.GetTestHarness();
        var orderPlaced = NewOrderPlaced(399.90m);

        await harness.Bus.Publish(orderPlaced);

        var paymentEvent = (await harness.WaitForPublishedEventsAsync<PaymentProcessedEvent>(e => e.OrderId == orderPlaced.OrderId))
            .Should().ContainSingle().Subject;
        paymentEvent.Status.Should().Be(PaymentStatus.Approved);

        var consumed = harness.Consumed.Select<OrderPlacedEvent>(message => message.Context.Message.OrderId == orderPlaced.OrderId).First();
        consumed.Context.ReceiveContext.InputAddress.AbsolutePath.Trim('/').Should().Be("payments.order-placed.limite-500");
    }

    private static OrderPlacedEvent NewOrderPlaced(decimal price)
        => new(Guid.CreateVersion7(), DateTime.UtcNow, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), price);

    private static string ReadSample(string fileName)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "_Fixtures", "Samples", fileName));

    private async Task<List<PaymentModel>> PaymentsOf(Guid orderId)
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        return await context.Set<PaymentModel>().AsNoTracking().Where(payment => payment.OrderId == orderId).ToListAsync();
    }
}
