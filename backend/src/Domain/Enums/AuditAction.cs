namespace Oypa.Crm.Domain.Enums;

/// <summary>Denetim kaydında gerçekleşen işlem türü.</summary>
public enum AuditAction
{
    /// <summary>Yeni kayıt oluşturuldu.</summary>
    Created = 1,

    /// <summary>Mevcut kayıt güncellendi.</summary>
    Updated = 2,

    /// <summary>Kayıt silindi (soft-delete dahil).</summary>
    Deleted = 3
}
