using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Fcg.Contracts.Security;
using Fcg.Payments.Domain.Payments;
using Fcg.Payments.Infrastructure._Shared.Context;
using Fcg.Payments.IntegrationTests._Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Fcg.Payments.IntegrationTests.WebApi.Payments.GetPaymentByOrderId;

[Collection(PaymentsApiCollection.Name)]
public class GetPaymentByOrderIdEndpointTests
{
    private readonly PaymentsApiFixture _fixture;

    public GetPaymentByOrderIdEndpointTests(PaymentsApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Get_DeveRetornarPagamento_QuandoUsuarioEDono()
    {
        var payment = await SeedAsync(399.90m);

        var response = await SendAsync(payment.OrderId, TestJwtFactory.Create(payment.UserId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        root.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(
            "paymentId", "orderId", "userId", "gameId", "amount", "status", "reason", "processedAt");
        root.GetProperty("paymentId").GetGuid().Should().Be(payment.Id());
        root.GetProperty("orderId").GetGuid().Should().Be(payment.OrderId);
        root.GetProperty("userId").GetGuid().Should().Be(payment.UserId);
        root.GetProperty("gameId").GetGuid().Should().Be(payment.GameId);
        root.GetProperty("amount").GetDecimal().Should().Be(399.90m);
        root.GetProperty("status").GetString().Should().Be("Rejected");
        root.GetProperty("reason").GetString().Should().Be(payment.Reason);
        root.GetProperty("processedAt").GetDateTime().Should().BeCloseTo(payment.ProcessedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Get_DeveRetornarPagamento_QuandoUsuarioAdministrador()
    {
        var payment = await SeedAsync(199.90m);

        var response = await SendAsync(payment.OrderId, TestJwtFactory.Create(Guid.CreateVersion7(), FcgRoles.Administrator));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("status").GetString().Should().Be("Approved");
        body.RootElement.GetProperty("reason").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Get_DeveRetornarNotFound_QuandoUsuarioNaoEDono()
    {
        var payment = await SeedAsync(199.90m);

        var response = await SendAsync(payment.OrderId, TestJwtFactory.Create(Guid.CreateVersion7()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Get_DeveRetornarNotFound_QuandoPagamentoInexistente()
    {
        var response = await SendAsync(Guid.CreateVersion7(), TestJwtFactory.Create(Guid.CreateVersion7(), FcgRoles.Administrator));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_DeveRetornarUnauthorized_QuandoSemToken()
    {
        var payment = await SeedAsync(199.90m);

        var response = await SendAsync(payment.OrderId, token: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpResponseMessage> SendAsync(Guid orderId, string? token)
    {
        using var client = _fixture.Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/payments/{orderId}");

        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await client.SendAsync(request);
    }

    private async Task<Payment> SeedAsync(decimal amount)
    {
        var payment = Payment.Process(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), amount, 300m);

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        context.Add(payment.PersistenceModel);
        await context.SaveChangesAsync();

        return payment;
    }
}
