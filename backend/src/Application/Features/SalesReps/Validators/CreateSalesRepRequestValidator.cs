using FluentValidation;
using Oypa.Crm.Contracts.SalesReps;

namespace Oypa.Crm.Application.Features.SalesReps.Validators;

public sealed class CreateSalesRepRequestValidator : AbstractValidator<CreateSalesRepRequest>
{
    public CreateSalesRepRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}
