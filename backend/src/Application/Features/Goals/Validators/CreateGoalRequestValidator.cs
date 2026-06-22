using FluentValidation;
using Oypa.Crm.Contracts.Goals;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Application.Features.Goals.Validators;

public sealed class CreateGoalRequestValidator : AbstractValidator<CreateGoalRequest>
{
    public CreateGoalRequestValidator()
    {
        RuleFor(x => x.AssigneeEmployeeId)
            .NotEmpty().WithMessage("Atanan personel zorunludur.");

        RuleFor(x => x.WeeklyTarget)
            .GreaterThan(0).WithMessage("Haftalık hedef 0'dan büyük olmalıdır.");

        RuleFor(x => x.Segment)
            .NotEmpty().WithMessage("Segment zorunludur.")
            .Must(s => Enum.TryParse<GoalSegment>(s, ignoreCase: true, out _))
            .WithMessage($"Segment değeri geçersiz. Geçerli değerler: {string.Join(", ", Enum.GetNames<GoalSegment>())}");
    }
}
