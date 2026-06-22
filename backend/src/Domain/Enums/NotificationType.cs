namespace Oypa.Crm.Domain.Enums;

/// <summary>Bildirimin nasıl/nereden üretildiğini sınıflandırır.</summary>
public enum NotificationType
{
    /// <summary>Yönetici/admin tarafından manuel olarak gönderildi.</summary>
    Manual,

    /// <summary>Yeni bir görüşme planlandığında otomatik tetiklendi.</summary>
    MeetingScheduled,

    /// <summary>Görüşmeye not eklendiğinde otomatik tetiklendi.</summary>
    MeetingNoteAdded,

    /// <summary>Çalışana yeni bir hedef atandığında otomatik tetiklendi.</summary>
    GoalAssigned,

    /// <summary>Lead müşteriye dönüştürüldüğünde otomatik tetiklendi.</summary>
    LeadConverted,

    /// <summary>İhale tarihine yaklaşıldığında atanan sorumluya otomatik tetiklendi.</summary>
    TenderApproaching
}
