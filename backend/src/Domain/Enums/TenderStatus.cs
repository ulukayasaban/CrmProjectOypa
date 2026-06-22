namespace Oypa.Crm.Domain.Enums;

/// <summary>İhale yaşam döngüsü durumu.</summary>
public enum TenderStatus
{
    /// <summary>İhale hazırlık aşamasında.</summary>
    Hazirlik,

    /// <summary>Teklif verilmiş, sonuç bekleniyor.</summary>
    TeklifVerildi,

    /// <summary>İhale kazanıldı.</summary>
    Kazanildi,

    /// <summary>İhale kaybedildi.</summary>
    Kaybedildi,

    /// <summary>İhale iptal edildi.</summary>
    Iptal
}
