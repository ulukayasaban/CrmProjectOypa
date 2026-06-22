using Microsoft.Extensions.DependencyInjection;
using Oypa.Crm.Application.Common.Events;
using Oypa.Crm.Domain.Common;

namespace Oypa.Crm.Infrastructure.Events;

/// <summary>Çalışma zamanı tipine göre kayıtlı <see cref="IDomainEventHandler{TEvent}"/>'ları bulup çağırır.</summary>
public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;

            foreach (var handler in serviceProvider.GetServices(handlerType))
            {
                if (handler is null) continue;
                await (Task)method.Invoke(handler, [domainEvent, cancellationToken])!;
            }
        }
    }
}
