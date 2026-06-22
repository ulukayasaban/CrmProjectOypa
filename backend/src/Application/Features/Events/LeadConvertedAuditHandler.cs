using Microsoft.Extensions.Logging;
using Oypa.Crm.Application.Common.Events;
using Oypa.Crm.Domain.Events;

namespace Oypa.Crm.Application.Features.Events;

/// <summary>Lead → müşteri dönüşümlerini denetim (audit) günlüğüne yazar.</summary>
public sealed class LeadConvertedAuditHandler(ILogger<LeadConvertedAuditHandler> logger)
    : IDomainEventHandler<LeadConvertedToCustomerEvent>
{
    public Task HandleAsync(LeadConvertedToCustomerEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Lead müşteriye dönüştürüldü. CompanyId={CompanyId} Title={Title}",
            domainEvent.CompanyId, domainEvent.CompanyTitle);
        return Task.CompletedTask;
    }
}
