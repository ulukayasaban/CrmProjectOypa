using FluentValidation;
using Oypa.Crm.Contracts.Tenders;

namespace Oypa.Crm.Application.Features.Tenders.Validators;

public sealed class ChangeTenderStatusRequestValidator : AbstractValidator<ChangeTenderStatusRequest>
{
    public ChangeTenderStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
