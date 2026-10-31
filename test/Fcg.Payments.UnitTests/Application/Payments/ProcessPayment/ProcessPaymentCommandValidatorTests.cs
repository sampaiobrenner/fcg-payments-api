using Fcg.Payments.Application.Payments.ProcessPayment;

namespace Fcg.Payments.UnitTests.Application.Payments.ProcessPayment;

public class ProcessPaymentCommandValidatorTests
{
    private readonly ProcessPaymentCommandValidator _validator = new();

    [Fact]
    public void Validate_DeveSerValido_QuandoIdsInformados()
    {
        _validator.Validate(new ProcessPaymentCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), 10m))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_DeveRetornarErros_QuandoIdsVazios()
    {
        var result = _validator.Validate(new ProcessPaymentCommand(Guid.Empty, Guid.Empty, Guid.Empty, 10m));

        result.Errors.Select(error => error.PropertyName).Should().BeEquivalentTo(
            nameof(ProcessPaymentCommand.OrderId), nameof(ProcessPaymentCommand.UserId), nameof(ProcessPaymentCommand.GameId));
        result.Errors.Should().AllSatisfy(error => error.ErrorMessage.Should().EndWith("é obrigatório."));
    }
}
