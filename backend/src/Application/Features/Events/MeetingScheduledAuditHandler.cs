using Microsoft.Extensions.Logging;
using Oypa.Crm.Application.Common.Events;
using Oypa.Crm.Domain.Events;

namespace Oypa.Crm.Application.Features.Events;

/// <summary>Planlanan görüşmeleri denetim (audit) günlüğüne yazar.</summary>
public sealed class MeetingScheduledAuditHandler(ILogger<MeetingScheduledAuditHandler> logger)
    : IDomainEventHandler<MeetingScheduledEvent>
{
    public Task HandleAsync(MeetingScheduledEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Görüşme planlandı. MeetingId={MeetingId} CompanyId={CompanyId} SalesRepId={SalesRepId}",
            domainEvent.MeetingId, domainEvent.CompanyId, domainEvent.SalesRepId);
        return Task.CompletedTask;
    }
}
