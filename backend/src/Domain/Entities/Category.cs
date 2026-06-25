using Oypa.Crm.Domain.Common;

namespace Oypa.Crm.Domain.Entities;

/// <summary>
/// Firma kategori etiketi. Soft-delete destekler; bir kategori silindiğinde firmalardan ilişkisi korunur
/// ancak category global query filter aracılığıyla sorgu dışı bırakılır.
/// </summary>
public class Category : BaseEntity, ISoftDelete
{
    // EF Core için
    private Category() { }

    public Category(string name, string color)
    {
        Name = name;
        Color = color;
    }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Hex renk kodu, ör: #3b82f6</summary>
    public string Color { get; private set; } = string.Empty;

    // ────────── ISoftDelete ──────────

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAtUtc { get; private set; }

    public void MarkDeleted(DateTime utcNow)
    {
        IsDeleted = true;
        DeletedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    // ────────── Domain Davranışı ──────────

    public void Rename(string name)
    {
        Name = name;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetColor(string color)
    {
        Color = color;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
