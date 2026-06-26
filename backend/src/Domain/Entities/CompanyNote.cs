using Oypa.Crm.Domain.Common;

namespace Oypa.Crm.Domain.Entities;

/// <summary>Bir firmaya eklenen zaman-damgalı, değiştirilemez not. Yazar adı snapshot olarak saklanır.</summary>
public sealed class CompanyNote : BaseEntity
{
    private CompanyNote() { }

    public CompanyNote(Guid companyId, string content, Guid? authorUserId, string authorName)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Not içeriği boş olamaz.", nameof(content));

        CompanyId = companyId;
        Content = content;
        AuthorUserId = authorUserId;
        AuthorName = authorName;
    }

    public Guid CompanyId { get; private set; }

    /// <summary>Not içeriği; notlar değiştirilemez.</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>Notun eklendiği andaki kullanıcı kimliği (boş olabilir — sistem notu).</summary>
    public Guid? AuthorUserId { get; private set; }

    /// <summary>Yazar adı snapshot — kullanıcı adı sonradan değişse bile tarihsel kayıt doğru kalır.</summary>
    public string AuthorName { get; private set; } = string.Empty;
}
