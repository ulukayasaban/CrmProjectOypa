using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Domain.Entities;

/// <summary>
/// Sistem genelinde varlık düzeyindeki değişikliklerin denetim kaydı.
/// BaseEntity'den türetilmez; kendi Id'si yeterlidir ve bu tablo soft-delete veya
/// domain event zinciri dışındadır.
/// </summary>
public sealed class AuditLog
{
    // EF Core için parametresiz kurucu
    private AuditLog() { }

    public AuditLog(
        string entityName,
        string entityId,
        AuditAction action,
        Guid? userId,
        string? userName,
        DateTime timestampUtc,
        string? changes)
    {
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        UserId = userId;
        UserName = userName;
        TimestampUtc = timestampUtc;
        Changes = changes;
    }

    /// <summary>Birincil anahtar.</summary>
    public long Id { get; private set; }

    /// <summary>Değişiklik yapılan entity'nin kısa sınıf adı (örn. "Company").</summary>
    public string EntityName { get; private set; } = string.Empty;

    /// <summary>Entity'nin birincil anahtar değeri (string olarak saklanır).</summary>
    public string EntityId { get; private set; } = string.Empty;

    /// <summary>Gerçekleşen işlem türü.</summary>
    public AuditAction Action { get; private set; }

    /// <summary>İşlemi yapan kullanıcının kimliği. Anonim/sistem işlemlerinde null.</summary>
    public Guid? UserId { get; private set; }

    /// <summary>İşlemi yapan kullanıcının e-posta veya adı.</summary>
    public string? UserName { get; private set; }

    /// <summary>İşlem UTC zaman damgası.</summary>
    public DateTime TimestampUtc { get; private set; }

    /// <summary>
    /// Değişen alanların kısa özeti (JSON veya düz metin).
    /// Büyük payload'ları kesmek için maksimum uzunluk uygulanır.
    /// </summary>
    public string? Changes { get; private set; }
}
