using Oypa.Crm.Domain.Common;

namespace Oypa.Crm.Domain.Entities;

/// <summary>Bir görüşmeye eklenen zaman-damgalı, değiştirilemez not. Yazar adı/ünvanı snapshot olarak saklanır.</summary>
public sealed class MeetingNote : BaseEntity
{
    private MeetingNote() { }

    public MeetingNote(Guid meetingId, string content, Guid? authorUserId, string authorName, string? authorTitle)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Not içeriği boş olamaz.", nameof(content));

        MeetingId = meetingId;
        Content = content;
        AuthorUserId = authorUserId;
        AuthorName = authorName;
        AuthorTitle = authorTitle;
    }

    public Guid MeetingId { get; private set; }

    /// <summary>Not içeriği; notlar değiştirilemez.</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>Notun eklendiği andaki kullanıcı kimliği (boş olabilir — sistem notu).</summary>
    public Guid? AuthorUserId { get; private set; }

    /// <summary>Yazar adı snapshot — kullanıcı adı sonradan değişse bile tarihsel kayıt doğru kalır.</summary>
    public string AuthorName { get; private set; } = string.Empty;

    /// <summary>Yazar ünvanı snapshot (null = ünvan bilgisi yok).</summary>
    public string? AuthorTitle { get; private set; }
}
