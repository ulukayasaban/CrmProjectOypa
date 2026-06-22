using Oypa.Crm.Domain.Common;

namespace Oypa.Crm.Application.Common.Events;

/// <summary>Belirli bir domain olayını işleyen bileşen.</summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
