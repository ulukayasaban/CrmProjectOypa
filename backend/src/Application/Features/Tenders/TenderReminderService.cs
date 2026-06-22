using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Features.Notifications;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Oypa.Crm.Application.Features.Tenders;

/// <summary>
/// Yaklaşan ihaleler için bildirim gönderir. Test edilebilirlik için Application katmanında tutulurkun;
/// zamanlama mantığı <c>Api/BackgroundServices/UpcomingTenderReminderHostedService</c>'te bulunur.
/// </summary>
public sealed class TenderReminderService(
    ITenderRepository tenders,
    IRepository<Employee> employees,
    INotificationService notificationService,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<TenderReminderService> logger) : ITenderReminderService
{
    public async Task<int> NotifyApproachingAsync(int daysAhead, CancellationToken cancellationToken = default)
    {
        var approaching = await tenders.ListApproachingAsync(clock.Today, daysAhead, cancellationToken);

        int processed = 0;

        foreach (var tender in approaching)
        {
            // AssignedSalesRep.Employee üzerinden ApplicationUserId çöz
            var salesRep = tender.AssignedSalesRep;
            if (salesRep?.EmployeeId is not { } employeeId)
            {
                logger.LogDebug(
                    "İhale {TenderId}: atanan temsilcinin Employee bağlantısı yok, atlanıyor.",
                    tender.Id);
                continue;
            }

            var employee = await employees.GetByIdAsync(employeeId, cancellationToken);
            if (employee?.ApplicationUserId is not { } userId)
            {
                logger.LogDebug(
                    "İhale {TenderId}: Employee {EmployeeId} için ApplicationUserId yok, atlanıyor.",
                    tender.Id, employeeId);
                continue;
            }

            var companyTitle = tender.Company?.Title ?? "Firma";
            // İhale tarihine kalan gerçek gün sayısı (sabit daysAhead değil).
            var daysLeft = tender.TenderDate.DayNumber - clock.Today.DayNumber;
            var message = daysLeft <= 0
                ? $"{companyTitle} — {tender.Title} ihalesi bugün."
                : $"{companyTitle} — {tender.Title} ihalesine {daysLeft} gün kaldı.";

            await notificationService.CreateForUsersAsync(
                [userId],
                message,
                NotificationType.TenderApproaching,
                title: "Yaklaşan İhale",
                link: "/tenders/aktif",
                cancellationToken: cancellationToken);

            tender.MarkApproachNotified(clock.UtcNow);
            processed++;
        }

        if (processed > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "TenderReminderService: {Count} ihale için yaklaşan bildirim gönderildi.",
            processed);

        return processed;
    }
}
