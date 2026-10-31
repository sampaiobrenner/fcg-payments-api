using Testcontainers.PostgreSql;

namespace Fcg.Payments.IntegrationTests._Fixtures;

/// <summary>Postgres compartilhado (Testcontainers) por todos os testes da colecao <see cref="PaymentsApiCollection"/>.</summary>
public sealed class PaymentsApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("fcg_payments")
        .WithUsername("fcg")
        .WithPassword("fcg")
        .Build();

    public PostgresPaymentsWebApplicationFactory Factory { get; private set; } = null!;

    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        Factory = CreateFactory();
        _ = Factory.Server;
    }

    public PostgresPaymentsWebApplicationFactory CreateFactory(IReadOnlyDictionary<string, string?>? settings = null)
        => new(ConnectionString, settings);

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class PaymentsApiCollection : ICollectionFixture<PaymentsApiFixture>
{
    public const string Name = "payments-api";
}
