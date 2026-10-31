using Fcg.Payments.Application;
using Fcg.Payments.Application.Payments._Shared;
using Fcg.Payments.Domain._Shared.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Fcg.Payments.UnitTests.Application.Payments._Shared;

public class PaymentsOptionsTests
{
    [Fact]
    public void ApprovalLimit_DeveSer300_QuandoNaoConfigurado()
    {
        Resolve(new Dictionary<string, string?>()).ApprovalLimit.Should().Be(300m);
    }

    [Fact]
    public void ApprovalLimit_DeveLerConfiguracao_QuandoPaymentsApprovalLimitInformado()
    {
        // Equivale a variavel de ambiente Payments__ApprovalLimit=450.50
        Resolve(new Dictionary<string, string?> { ["Payments:ApprovalLimit"] = "450.50" }).ApprovalLimit.Should().Be(450.50m);
    }

    private static PaymentsOptions Resolve(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection().AddModule<FcgPaymentsApplicationModule>(configuration);

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<PaymentsOptions>>().Value;
    }
}
