using FluentValidation;
using Oypa.Crm.Contracts.Notifications;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Application.Features.Notifications.Validators;

public sealed class UpdateNotificationPreferencesRequestValidator
    : AbstractValidator<UpdateNotificationPreferencesRequest>
{
    public UpdateNotificationPreferencesRequestValidator()
    {
        RuleFor(r => r.Items)
            .NotNull().WithMessage("Tercih listesi boş olamaz.");

        RuleForEach(r => r.Items)
            .Must(item => item.Type != NotificationType.Manual)
            .WithMessage("Manual bildirim türü tercihe tabi değildir; bu tip gönderilemez.")
            .Must(item => Enum.IsDefined(typeof(NotificationType), item.Type))
            .WithMessage("Geçersiz bildirim türü.");
    }
}
