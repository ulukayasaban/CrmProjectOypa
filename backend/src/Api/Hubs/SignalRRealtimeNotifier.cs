using Microsoft.AspNetCore.SignalR;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Contracts.Notifications;

namespace Oypa.Crm.Api.Hubs;

/// <summary>
/// <see cref="IRealtimeNotifier"/> implementasyonu. SignalR üzerinden bağlı istemcilere anlık push yapar.
/// Bağlı olmayan kullanıcılar için mesaj sessizce atlanır; polling fallback'i mevcuttur.
/// </summary>
public sealed class SignalRRealtimeNotifier(
    IHubContext<NotificationsHub> hubContext) : IRealtimeNotifier
{
    public Task NotifyUsersAsync(
        IEnumerable<Guid> userIds,
        NotificationDto payload,
        CancellationToken cancellationToken = default)
    {
        // Clients.Users() string listesi bekler; sub claim Guid.ToString() ile eşleşmeli
        var userIdStrings = userIds.Select(id => id.ToString()).ToList();

        if (userIdStrings.Count == 0)
            return Task.CompletedTask;

        return hubContext.Clients
            .Users(userIdStrings)
            .SendAsync("ReceiveNotification", payload, cancellationToken);
    }
}
