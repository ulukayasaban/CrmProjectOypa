namespace Oypa.Crm.Domain.Common;

/// <summary>
/// Fiziksel silme yerine mantıksal silmeyi destekleyen entity'ler için işaretçi arayüzü.
/// <see cref="IsDeleted"/> true ise kayıt global query filter tarafından sorgu dışı bırakılır.
/// </summary>
public interface ISoftDelete
{
    /// <summary>Kaydın silinmiş olarak işaretlenip işaretlenmediği.</summary>
    bool IsDeleted { get; }

    /// <summary>Silme zaman damgası (UTC). Henüz silinmediyse null.</summary>
    DateTime? DeletedAtUtc { get; }

    /// <summary>Kaydı silinmiş olarak işaretler.</summary>
    void MarkDeleted(DateTime utcNow);

    /// <summary>Silme işlemini geri alır (kaydı geri yükler).</summary>
    void Restore();
}
