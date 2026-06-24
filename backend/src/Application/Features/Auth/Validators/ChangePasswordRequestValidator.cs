using FluentValidation;
using Oypa.Crm.Contracts.Auth;

namespace Oypa.Crm.Application.Features.Auth.Validators;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Mevcut parola boş olamaz.");

        // Yeni parola, Identity politikasıyla uyumlu: min 8, büyük/küçük/rakam/özel karakter.
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Yeni parola en az 8 karakter olmalıdır.")
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Yeni parola en az bir büyük harf içermelidir.")
            .Matches("[a-z]").WithMessage("Yeni parola en az bir küçük harf içermelidir.")
            .Matches("[0-9]").WithMessage("Yeni parola en az bir rakam içermelidir.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Yeni parola en az bir özel karakter içermelidir.");
    }
}
