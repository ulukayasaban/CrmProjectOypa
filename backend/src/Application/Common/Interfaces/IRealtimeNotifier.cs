using Oypa.Crm.Contracts.Notifications;

namespace Oypa.Crm.Application.Common.Interfaces;

/// <summary>
/// Gerçek zamanlı bildirim push mekanizmasını soyutlar (SignalR veya başka bir taşıma).
/// Implementasyon Api katmanında yer alır (Clean Architecture).
/// </summary>
public interface IRealtimeNotifier
{
    /// <summary>
    /// Belirtilen kullanıcı Id'lerine anlık olarak bildirim iletir.
    /// Bağlantı yoksa işlem sessizce atlanır.
    /// </summary>
    Task NotifyUsersAsync(
        IEnumerable<Guid> userIds,
        NotificationDto payload,
        CancellationToken cancellationToken = default);
}
