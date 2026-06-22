using Oypa.Crm.Domain.Common;

namespace Oypa.Crm.Domain.Events;

/// <summary>Bir lead müşteriye dönüştürüldüğünde tetiklenir.</summary>
public sealed record LeadConvertedToCustomerEvent(
    Guid CompanyId,
    string CompanyTitle) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
