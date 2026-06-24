using Oypa.Crm.Domain.Common;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Domain.Entities;

/// <summary>
/// Bir kullanıcının belirli bir bildirim türü için açma/kapama tercihini tutar.
/// Opt-out modeli: kayıt yoksa varsayılan ETKİN kabul edilir.
/// Manual tipi tercihe tabi değildir — her zaman teslim edilir.
/// </summary>
public sealed class NotificationPreference : BaseEntity
{
    // EF Core için parametresiz constructor.
    private NotificationPreference() { }

    private NotificationPreference(Guid userId, NotificationType type, bool enabled)
    {
        UserId = userId;
        Type = type;
        Enabled = enabled;
    }

    /// <summary>Tercihin ait olduğu kullanıcı (ApplicationUser.Id).</summary>
    public Guid UserId { get; private set; }

    /// <summary>Tercih edilen bildirim türü (Manual hariç).</summary>
    public NotificationType Type { get; private set; }

    /// <summary>Bu bildirim türü kullanıcı tarafından etkin mi?</summary>
    public bool Enabled { get; private set; }

    /// <summary>Yeni bir tercih kaydı oluşturur.</summary>
    public static NotificationPreference Create(Guid userId, NotificationType type, bool enabled) =>
        new(userId, type, enabled);

    /// <summary>Tercihin etkinlik durumunu günceller.</summary>
    public void SetEnabled(bool enabled)
    {
        Enabled = enabled;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
