using FluentValidation;
using Oypa.Crm.Contracts.Employees;

namespace Oypa.Crm.Application.Features.Employees.Validators;

public sealed class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    private static readonly string[] ValidRoles = ["Admin", "Sales"];

    public CreateEmployeeRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Email)
            .EmailAddress()
            .MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        // Hesap oluşturulacaksa e-posta ve rol zorunlu
        When(x => x.CreateAccount, () =>
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Hesap oluşturmak için e-posta zorunludur.")
                .EmailAddress()
                .MaximumLength(256);

            RuleFor(x => x.Role)
                .NotEmpty()
                .WithMessage("Hesap oluşturmak için rol zorunludur.")
                .Must(r => ValidRoles.Contains(r))
                .WithMessage("Rol 'Admin' veya 'Sales' olmalıdır.");
        });
    }
}
