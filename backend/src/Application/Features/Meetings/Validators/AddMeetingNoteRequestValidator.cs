using FluentValidation;
using Oypa.Crm.Contracts.Meetings;

namespace Oypa.Crm.Application.Features.Meetings.Validators;

public sealed class AddMeetingNoteRequestValidator : AbstractValidator<AddMeetingNoteRequest>
{
    public AddMeetingNoteRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Not içeriği boş olamaz.")
            .MaximumLength(2000).WithMessage("Not içeriği en fazla 2000 karakter olabilir.");
    }
}
