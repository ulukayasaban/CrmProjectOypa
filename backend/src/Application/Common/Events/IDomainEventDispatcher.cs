using Oypa.Crm.Domain.Common;

namespace Oypa.Crm.Application.Common.Events;

/// <summary>Toplanan domain olaylarını ilgili handler'lara dağıtır.</summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
