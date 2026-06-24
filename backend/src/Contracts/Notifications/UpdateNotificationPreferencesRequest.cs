using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Contracts.Notifications;

/// <summary>Bir bildirim türü için toggle bilgisi.</summary>
public sealed record NotificationPreferenceItem(
    NotificationType Type,
    bool Enabled);

/// <summary>Geçerli kullanıcının bildirim tercihlerini toplu güncelleme isteği.</summary>
public sealed record UpdateNotificationPreferencesRequest(
    IReadOnlyList<NotificationPreferenceItem> Items);
