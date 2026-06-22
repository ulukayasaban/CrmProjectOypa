using FluentValidation;
using Oypa.Crm.Contracts.Employees;

namespace Oypa.Crm.Application.Features.Employees.Validators;

public sealed class AssignRoleRequestValidator : AbstractValidator<AssignRoleRequest>
{
    private static readonly string[] ValidRoles = ["Admin", "Sales"];

    public AssignRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(r => ValidRoles.Contains(r))
            .WithMessage("Rol 'Admin' veya 'Sales' olmalıdır.");
    }
}
