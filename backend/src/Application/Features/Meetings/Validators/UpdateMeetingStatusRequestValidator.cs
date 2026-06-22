using FluentValidation;
using Oypa.Crm.Contracts.Meetings;

namespace Oypa.Crm.Application.Features.Meetings.Validators;

public sealed class UpdateMeetingStatusRequestValidator : AbstractValidator<UpdateMeetingStatusRequest>
{
    public UpdateMeetingStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Comment).MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Comment));
    }
}
