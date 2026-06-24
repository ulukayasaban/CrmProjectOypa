using FluentValidation;
using Oypa.Crm.Contracts.Auth;

namespace Oypa.Crm.Application.Features.Auth.Validators;

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta adresi boş olamaz.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .MaximumLength(256);

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Sıfırlama jetonu boş olamaz.");

        // Identity politikasıyla uyumlu parola kuralları.
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
