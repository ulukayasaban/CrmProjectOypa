using Oypa.Crm.Domain.Common;

namespace Oypa.Crm.Domain.Entities;

/// <summary>
/// Bir firmaya bağlı ilgili kişi.
/// Soft-delete destekler; fiziksel silme yerine <see cref="ISoftDelete.MarkDeleted"/> kullanılır.
/// </summary>
public class Contact : BaseEntity, ISoftDelete
{
    private Contact() { }

    public Contact(Guid companyId, string name, string? email, string? phone)
    {
        CompanyId = companyId;
        Name = name;
        Email = email;
        Phone = phone;
    }

    public Guid CompanyId { get; private set; }
    public Company? Company { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }

    public void Update(string name, string? email, string? phone)
    {
        Name = name;
        Email = email;
        Phone = phone;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    // ────────── ISoftDelete ──────────

    /// <summary>İlgili kişi silinmiş olarak işaretlenmiş mi.</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>Silme zaman damgası (UTC). Silinmediyse null.</summary>
    public DateTime? DeletedAtUtc { get; private set; }

    /// <summary>İlgili kişiyi mantıksal olarak siler.</summary>
    public void MarkDeleted(DateTime utcNow)
    {
        IsDeleted = true;
        DeletedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>Silinmiş ilgili kişiyi geri yükler.</summary>
    public void Restore()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
