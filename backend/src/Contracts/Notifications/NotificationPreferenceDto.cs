namespace Oypa.Crm.Contracts.Notifications;

/// <summary>Tek bir bildirim türünün kullanıcı tercihini temsil eder.</summary>
public sealed record NotificationPreferenceDto(
    string Type,
    bool Enabled);
