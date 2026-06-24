using Oypa.Crm.Contracts.Notifications;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Application.Features.Notifications;

/// <summary>Bildirim tür-tercihlerini yöneten servis sözleşmesi.</summary>
public interface INotificationPreferenceService
{
    /// <summary>
    /// Geçerli kullanıcının 5 toggle edilebilir tip için mevcut tercihlerini döndürür.
    /// Kayıt bulunmayan tipler için Enabled=true (opt-out modeli varsayılanı) döner.
    /// </summary>
    Task<IReadOnlyList<NotificationPreferenceDto>> GetMineAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Geçerli kullanıcının bildirim tercihlerini toplu günceller (upsert).
    /// Manual tipi içeren öğeler yok sayılır.
    /// </summary>
    Task SetMineAsync(IReadOnlyList<NotificationPreferenceItem> items, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verilen kullanıcı kümesinden belirtilen bildirim türü için ETKİN olanları döndürür.
    /// Manual tipi için giriş listesinin tamamı döner (tercih filtrelemesi yapılmaz).
    /// Diğer tipler için Enabled=false kaydı olan kullanıcılar çıkarılır;
    /// kaydı bulunmayanlar varsayılan olarak etkin kabul edilir.
    /// </summary>
    Task<IReadOnlyList<Guid>> IsEnabledForUsersAsync(
        IEnumerable<Guid> userIds,
        NotificationType type,
        CancellationToken cancellationToken = default);
}
