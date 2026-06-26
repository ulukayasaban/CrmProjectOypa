using FluentValidation;
using Oypa.Crm.Contracts.Companies;

namespace Oypa.Crm.Application.Features.Companies.Validators;

public sealed class CreateCompanyNoteRequestValidator : AbstractValidator<CreateCompanyNoteRequest>
{
    public CreateCompanyNoteRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Not içeriği boş olamaz.")
            .MaximumLength(2000).WithMessage("Not içeriği en fazla 2000 karakter olabilir.");
    }
}
