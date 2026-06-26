using FluentValidation;
using Oypa.Crm.Contracts.Companies;

namespace Oypa.Crm.Application.Features.Companies.Validators;

public sealed class UpdateCompanyRequestValidator : AbstractValidator<UpdateCompanyRequest>
{
    public UpdateCompanyRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Sector).IsInEnum();
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(400);

        RuleFor(x => x.City).MaximumLength(100).When(x => x.City != null);
        RuleFor(x => x.Website).MaximumLength(200).When(x => x.Website != null);
        RuleFor(x => x.TaxNumber).MaximumLength(20).When(x => x.TaxNumber != null);
        RuleFor(x => x.Source).IsInEnum().When(x => x.Source != null);

        // Yeni alanlar
        RuleFor(x => x.ServiceSector).IsInEnum().When(x => x.ServiceSector != null);
        RuleFor(x => x.FirmType).IsInEnum();
        RuleFor(x => x.SourceNote).MaximumLength(500).When(x => x.SourceNote != null);
    }
}
