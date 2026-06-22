using Oypa.Crm.Contracts.Notifications;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Application.Features.Notifications;

/// <summary>Per-alıcı bildirim yönetimi: okuma, işaretleme, manuel gönderim ve dahili üretim.</summary>
public interface INotificationService
{
    /// <summary>Geçerli kullanıcının bildirimlerini tarihe göre azalan sırada döndürür.</summary>
    Task<IReadOnlyList<NotificationDto>> GetMineAsync(CancellationToken cancellationToken = default);

    /// <summary>Geçerli kullanıcının okunmamış bildirim sayısını döndürür.</summary>
    Task<int> GetMyUnreadCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirtilen bildirimi okundu olarak işaretler.
    /// Bildirim geçerli kullanıcıya ait değilse NotFoundException fırlatır.
    /// </summary>
    Task MarkReadAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Geçerli kullanıcının tüm okunmamış bildirimlerini okundu olarak işaretler.</summary>
    Task MarkAllMineReadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Manuel birim gönderimi: hedef birimin alt-ağacındaki hesaplı kullanıcılara bildirim gönderir.
    /// Yetki: Admin veya astı olan yönetici. Sales kullanıcıları için ForbiddenAppException fırlatır.
    /// </summary>
    Task SendToUnitAsync(SendNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dahili yardımcı: verilen kullanıcı listesi için bildirim satırları oluşturur ve gerçek zamanlı iletir.
    /// Olay handler'ları ve SendToUnitAsync bu metodu kullanır.
    /// </summary>
    Task CreateForUsersAsync(
        IEnumerable<Guid> userIds,
        string message,
        NotificationType type,
        string? title = null,
        string? link = null,
        Guid? senderUserId = null,
        string? senderName = null,
        CancellationToken cancellationToken = default);
}
