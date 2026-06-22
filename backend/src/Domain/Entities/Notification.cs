using Oypa.Crm.Domain.Common;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Domain.Entities;

/// <summary>
/// Belirli bir alıcıya gönderilen per-kullanıcı sistem bildirimi.
/// Her alıcı için ayrı satır tutulur; böylece okundu durumu izole olur.
/// </summary>
public class Notification : BaseEntity
{
    private Notification() { }

    public Notification(
        Guid recipientUserId,
        string message,
        NotificationType type,
        string? title = null,
        Guid? senderUserId = null,
        string? senderName = null,
        string? link = null)
    {
        RecipientUserId = recipientUserId;
        Message = message;
        Type = type;
        Title = title;
        SenderUserId = senderUserId;
        SenderName = senderName;
        Link = link;
    }

    /// <summary>Bildirimin iletildiği kullanıcı (ApplicationUser.Id).</summary>
    public Guid RecipientUserId { get; private set; }

    /// <summary>Bildirim mesajı.</summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>Opsiyonel başlık (özet veya başlık metni).</summary>
    public string? Title { get; private set; }

    /// <summary>Bildirimin türü; UI'da ikon ve renk için kullanılır.</summary>
    public NotificationType Type { get; private set; }

    /// <summary>Bildirimi tetikleyen kullanıcının Id'si (null = sistem/otomatik).</summary>
    public Guid? SenderUserId { get; private set; }

    /// <summary>Gönderenin adının anlık görüntüsü (silinen hesaplar için koruma).</summary>
    public string? SenderName { get; private set; }

    /// <summary>FE yönlendirme bağlantısı (örn. "/companies/{id}"). Null ise yönlendirme yok.</summary>
    public string? Link { get; private set; }

    /// <summary>Bu alıcı tarafından okundu mu?</summary>
    public bool IsRead { get; private set; }

    /// <summary>Bildirimi okundu olarak işaretler.</summary>
    public void MarkRead()
    {
        IsRead = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
