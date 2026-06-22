using Microsoft.Extensions.Logging;
using Oypa.Crm.Application.Common.Events;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Features.Notifications;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Oypa.Crm.Domain.Events;

namespace Oypa.Crm.Application.Features.Events;

/// <summary>
/// Görüşme planlandığında SalesRep'in yönetici zincirine (ve mümkünse reppin kendisine)
/// otomatik bildirim üretir.
/// </summary>
public sealed class MeetingScheduledNotificationHandler(
    IOrgScopeService orgScope,
    IRepository<SalesRep> salesReps,
    IRepository<Employee> employees,
    INotificationService notificationService,
    ILogger<MeetingScheduledNotificationHandler> logger)
    : IDomainEventHandler<MeetingScheduledEvent>
{
    public async Task HandleAsync(MeetingScheduledEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var rep = await salesReps.GetByIdAsync(domainEvent.SalesRepId, cancellationToken);
        if (rep is null)
        {
            logger.LogWarning(
                "MeetingScheduledNotification: SalesRep {SalesRepId} bulunamadı.",
                domainEvent.SalesRepId);
            return;
        }

        if (rep.EmployeeId is null)
        {
            // Rep org yapısına bağlı değil; bildirim üretilemiyor
            return;
        }

        // Yönetici zincirindeki hesaplı kullanıcılar
        var ancestorUserIds = await orgScope.GetAncestorUserIdsAsync(rep.EmployeeId.Value, cancellationToken);

        // Reppin kendi bağlı ApplicationUserId'si
        var repEmployee = await employees.GetByIdAsync(rep.EmployeeId.Value, cancellationToken);
        var repUserId = repEmployee?.ApplicationUserId;

        var allRecipients = ancestorUserIds
            .Concat(repUserId.HasValue ? [repUserId.Value] : [])
            .Distinct();

        var message = $"Görüşme planlandı (Görüşme Id: {domainEvent.MeetingId}).";
        var link = $"/companies/{domainEvent.CompanyId}";

        await notificationService.CreateForUsersAsync(
            allRecipients,
            message,
            NotificationType.MeetingScheduled,
            title: "Görüşme Planlandı",
            link: link,
            senderUserId: null,
            senderName: null,
            cancellationToken);

        logger.LogInformation(
            "MeetingScheduledNotification: MeetingId={MeetingId} için bildirim üretildi.",
            domainEvent.MeetingId);
    }
}
