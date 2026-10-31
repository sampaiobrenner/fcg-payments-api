using Fcg.Payments.Application.Properties;
using FluentValidation;

namespace Fcg.Payments.Application.Payments.ProcessPayment;

internal sealed class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentCommandValidator()
    {
        RuleFor(command => command.OrderId).NotEmpty().WithMessage(Required(nameof(ProcessPaymentCommand.OrderId)));
        RuleFor(command => command.UserId).NotEmpty().WithMessage(Required(nameof(ProcessPaymentCommand.UserId)));
        RuleFor(command => command.GameId).NotEmpty().WithMessage(Required(nameof(ProcessPaymentCommand.GameId)));
    }

    private static string Required(string field) => string.Format(ApplicationResources.CampoObrigatorio, field);
}
