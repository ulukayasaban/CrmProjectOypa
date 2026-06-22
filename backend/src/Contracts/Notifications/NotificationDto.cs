namespace Oypa.Crm.Contracts.Notifications;

/// <summary>Tek bir bildirim kaydının istemciye aktarılan görünümü.</summary>
public sealed record NotificationDto(
    Guid Id,
    string? Title,
    string Message,
    string Type,
    string? SenderName,
    string? Link,
    bool IsRead,
    DateTime CreatedAtUtc);
