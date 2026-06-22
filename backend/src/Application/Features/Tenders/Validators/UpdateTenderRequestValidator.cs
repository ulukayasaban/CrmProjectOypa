using FluentValidation;
using Oypa.Crm.Contracts.Tenders;

namespace Oypa.Crm.Application.Features.Tenders.Validators;

public sealed class UpdateTenderRequestValidator : AbstractValidator<UpdateTenderRequest>
{
    public UpdateTenderRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(250);
        RuleFor(x => x.TenderNumber).MaximumLength(60).When(x => x.TenderNumber is not null);
        RuleFor(x => x.Sector).IsInEnum();
        RuleFor(x => x.TenderDate).NotEmpty();
        RuleFor(x => x.PersonnelCount).GreaterThanOrEqualTo(0).When(x => x.PersonnelCount.HasValue);
        RuleFor(x => x.EstimatedValue).GreaterThanOrEqualTo(0m).When(x => x.EstimatedValue.HasValue);
        RuleFor(x => x.Volume).GreaterThanOrEqualTo(0m).When(x => x.Volume.HasValue);
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0).When(x => x.Quantity.HasValue);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
    }
}
