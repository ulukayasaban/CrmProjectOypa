using FluentValidation;
using Oypa.Crm.Contracts.Notifications;

namespace Oypa.Crm.Application.Features.Notifications.Validators;

public sealed class SendNotificationRequestValidator : AbstractValidator<SendNotificationRequest>
{
    public SendNotificationRequestValidator()
    {
        RuleFor(r => r.TargetUnitId)
            .NotEmpty().WithMessage("Hedef birim seçilmesi zorunludur.");

        RuleFor(r => r.Message)
            .NotEmpty().WithMessage("Bildirim mesajı boş olamaz.")
            .MaximumLength(500).WithMessage("Mesaj en fazla 500 karakter olabilir.");

        RuleFor(r => r.Title)
            .MaximumLength(200).WithMessage("Başlık en fazla 200 karakter olabilir.")
            .When(r => r.Title is not null);
    }
}
