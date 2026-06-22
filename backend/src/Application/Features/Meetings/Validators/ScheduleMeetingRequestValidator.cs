using FluentValidation;
using Oypa.Crm.Contracts.Meetings;

namespace Oypa.Crm.Application.Features.Meetings.Validators;

public sealed class ScheduleMeetingRequestValidator : AbstractValidator<ScheduleMeetingRequest>
{
    public ScheduleMeetingRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.SalesRepId).NotEmpty();
        RuleFor(x => x.Method).IsInEnum();
        RuleFor(x => x.Address).NotEmpty().MaximumLength(400);
        RuleFor(x => x.Date).NotEmpty();
    }
}
