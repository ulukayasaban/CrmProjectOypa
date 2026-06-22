using Oypa.Crm.Domain.Common;

namespace Oypa.Crm.Domain.Events;

/// <summary>Yeni bir görüşme planlandığında tetiklenir (hatırlatma maili + bildirim için).</summary>
public sealed record MeetingScheduledEvent(
    Guid MeetingId,
    Guid CompanyId,
    Guid SalesRepId,
    Guid? ContactId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
