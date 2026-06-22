namespace Oypa.Crm.Contracts.Notifications;

/// <summary>
/// Manuel birim bildirim gönderim isteği.
/// TargetUnitId, alt-ağacındaki hesaplı tüm kullanıcılara iletilir.
/// </summary>
public sealed record SendNotificationRequest(
    Guid TargetUnitId,
    string? Title,
    string Message);
