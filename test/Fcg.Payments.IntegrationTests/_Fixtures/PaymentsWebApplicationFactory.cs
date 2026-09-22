using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace Fcg.Payments.IntegrationTests._Fixtures;

public sealed class PaymentsWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", "Host=localhost;Port=5432;Database=fcg_integration_tests;Username=test;Password=test");
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");
        builder.UseSetting("Jwt:Key", new string('k', 32));

        builder.ConfigureTestServices(services => services.AddMassTransitTestHarness());
    }
}
