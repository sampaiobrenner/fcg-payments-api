using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace Fcg.Payments.IntegrationTests._Fixtures;

/// <summary>
/// Sobe a API contra o Postgres do Testcontainers (migrations aplicadas no startup) e troca o RabbitMQ
/// pelo transporte em memoria do <see cref="ITestHarness"/>, mantendo o EF Outbox/Inbox real.
/// </summary>
public sealed class PostgresPaymentsWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly IReadOnlyDictionary<string, string?> _settings;

    public PostgresPaymentsWebApplicationFactory(string connectionString, IReadOnlyDictionary<string, string?>? settings = null)
    {
        _connectionString = connectionString;
        _settings = settings ?? new Dictionary<string, string?>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _connectionString);
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Jwt:Key", TestJwtFactory.Key);
        builder.UseSetting("Jwt:Issuer", TestJwtFactory.Issuer);
        builder.UseSetting("Jwt:Audience", TestJwtFactory.Audience);

        foreach (var (key, value) in _settings)
            builder.UseSetting(key, value);

        builder.ConfigureTestServices(services => services.AddMassTransitTestHarness(harness =>
            harness.SetTestTimeouts(testTimeout: TimeSpan.FromSeconds(30), testInactivityTimeout: TimeSpan.FromSeconds(5))));
    }
}
