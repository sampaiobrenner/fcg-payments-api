using Fcg.Payments.Application.Properties;
using FluentValidation;

namespace Fcg.Payments.Application.Payments.GetPaymentByOrderId;

internal sealed class GetPaymentByOrderIdQueryValidator : AbstractValidator<GetPaymentByOrderIdQuery>
{
    public GetPaymentByOrderIdQueryValidator()
    {
        RuleFor(query => query.OrderId)
            .NotEmpty()
            .WithMessage(string.Format(ApplicationResources.CampoObrigatorio, nameof(GetPaymentByOrderIdQuery.OrderId)));
    }
}
