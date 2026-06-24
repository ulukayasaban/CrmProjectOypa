using FluentValidation;
using Oypa.Crm.Contracts.Auth;

namespace Oypa.Crm.Application.Features.Auth.Validators;

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Ad Soyad boş olamaz.")
            .MaximumLength(150).WithMessage("Ad Soyad en fazla 150 karakter olabilir.");

        RuleFor(x => x.Phone)
            .MaximumLength(30).WithMessage("Telefon numarası en fazla 30 karakter olabilir.")
            .When(x => x.Phone is not null);

        RuleFor(x => x.Position)
            .MaximumLength(100).WithMessage("Pozisyon en fazla 100 karakter olabilir.")
            .When(x => x.Position is not null);
    }
}
