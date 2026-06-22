using Oypa.Crm.Contracts.Notifications;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Application.Mappings;

public static class NotificationMappings
{
    public static NotificationDto ToDto(this Notification n) => new(
        n.Id,
        n.Title,
        n.Message,
        n.Type.ToString(),
        n.SenderName,
        n.Link,
        n.IsRead,
        n.CreatedAtUtc);
}
