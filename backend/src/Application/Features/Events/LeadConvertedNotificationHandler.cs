using Microsoft.Extensions.Logging;
using Oypa.Crm.Application.Common.Events;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Features.Notifications;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Oypa.Crm.Domain.Events;

namespace Oypa.Crm.Application.Features.Events;

/// <summary>
/// Lead müşteriye dönüştürüldüğünde atanan SalesRep'in yönetici zincirine otomatik bildirim üretir.
/// </summary>
public sealed class LeadConvertedNotificationHandler(
    IOrgScopeService orgScope,
    IRepository<Company> companies,
    IRepository<SalesRep> salesReps,
    INotificationService notificationService,
    ILogger<LeadConvertedNotificationHandler> logger)
    : IDomainEventHandler<LeadConvertedToCustomerEvent>
{
    public async Task HandleAsync(LeadConvertedToCustomerEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var company = await companies.GetByIdAsync(domainEvent.CompanyId, cancellationToken);
        if (company is null)
        {
            logger.LogWarning(
                "LeadConvertedNotification: Firma {CompanyId} bulunamadı.",
                domainEvent.CompanyId);
            return;
        }

        if (company.AssignedSalesRepId is null)
        {
            // Atanan rep yok; bildirim üretilemiyor
            return;
        }

        var rep = await salesReps.GetByIdAsync(company.AssignedSalesRepId.Value, cancellationToken);
        if (rep?.EmployeeId is null)
            return;

        // Reppin yönetici zinciri
        var ancestorUserIds = await orgScope.GetAncestorUserIdsAsync(rep.EmployeeId.Value, cancellationToken);

        if (ancestorUserIds.Count == 0)
            return;

        var message = $"{domainEvent.CompanyTitle} müşteriye dönüştü.";
        var link = $"/companies/{domainEvent.CompanyId}";

        await notificationService.CreateForUsersAsync(
            ancestorUserIds,
            message,
            NotificationType.LeadConverted,
            title: "Lead Dönüştü",
            link: link,
            senderUserId: null,
            senderName: null,
            cancellationToken);

        logger.LogInformation(
            "LeadConvertedNotification: CompanyId={CompanyId} için {Count} alıcıya bildirim gönderildi.",
            domainEvent.CompanyId, ancestorUserIds.Count);
    }
}
