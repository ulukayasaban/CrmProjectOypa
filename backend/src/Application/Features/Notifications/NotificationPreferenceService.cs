using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Contracts.Notifications;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Application.Features.Notifications;

/// <summary>
/// Bildirim tür-tercihlerini yöneten uygulama servisi.
/// Opt-out modeli: DB'de kaydı olmayan tipler etkin kabul edilir.
/// </summary>
public sealed class NotificationPreferenceService(
    IRepository<NotificationPreference> preferences,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : INotificationPreferenceService
{
    /// <summary>
    /// Manual hariç toggle edilebilir 5 tip; sıralı ve sabit.
    /// Bu listedeki her tip için kullanıcıya bir tercih satırı sunulur.
    /// </summary>
    private static readonly IReadOnlyList<NotificationType> ToggleableTypes =
    [
        NotificationType.MeetingScheduled,
        NotificationType.MeetingNoteAdded,
        NotificationType.GoalAssigned,
        NotificationType.LeadConverted,
        NotificationType.TenderApproaching,
    ];

    // -----------------------------------------------------------------------
    // Sorgular
    // -----------------------------------------------------------------------

    public async Task<IReadOnlyList<NotificationPreferenceDto>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();

        // Mevcut kayıtları çek (AsNoTracking — salt okunur sorgu).
        var existingRows = await preferences.ListAsync(
            p => p.UserId == userId,
            cancellationToken);

        var lookup = existingRows.ToDictionary(p => p.Type, p => p.Enabled);

        // Toggle edilebilir 5 tipin tamamını döndür; kayıt yoksa Enabled=true (varsayılan opt-out modeli).
        return ToggleableTypes
            .Select(t => new NotificationPreferenceDto(
                t.ToString(),
                lookup.GetValueOrDefault(t, true)))
            .ToList();
    }

    // -----------------------------------------------------------------------
    // Mutasyonlar
    // -----------------------------------------------------------------------

    public async Task SetMineAsync(IReadOnlyList<NotificationPreferenceItem> items, CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();

        // Manual tipi tercihe tabi değil — yok say.
        var validItems = items
            .Where(i => i.Type != NotificationType.Manual)
            .ToList();

        if (validItems.Count == 0)
            return;

        // Mevcut kayıtları takipli (tracked) çek; güncelleme gerektirebilir.
        var existingRows = await preferences.ListAsync(
            p => p.UserId == userId,
            cancellationToken);

        // Tracked olmayan ListAsync kullanıldığından Update çağrısı gerekir.
        var existingLookup = existingRows.ToDictionary(p => p.Type);

        foreach (var item in validItems)
        {
            if (existingLookup.TryGetValue(item.Type, out var existing))
            {
                // Mevcut kaydı güncelle (değişmedi bile olsa idempotent).
                existing.SetEnabled(item.Enabled);
                preferences.Update(existing);
            }
            else
            {
                // Yeni tercih kaydı oluştur.
                var newPref = NotificationPreference.Create(userId, item.Type, item.Enabled);
                await preferences.AddAsync(newPref, cancellationToken);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // -----------------------------------------------------------------------
    // Dahili yardımcı — CreateForUsersAsync filtresi için
    // -----------------------------------------------------------------------

    public async Task<IReadOnlyList<Guid>> IsEnabledForUsersAsync(
        IEnumerable<Guid> userIds,
        NotificationType type,
        CancellationToken cancellationToken = default)
    {
        var idList = userIds.Distinct().ToList();

        // Manual tipi için tercih filtrelemesi yapılmaz; tüm kullanıcılar teslim alır.
        if (type == NotificationType.Manual)
            return idList;

        // Enabled=false olan kayıtları bul (opt-out edenler).
        var disabledUserIds = (await preferences.ListAsync(
                p => idList.Contains(p.UserId) && p.Type == type && !p.Enabled,
                cancellationToken))
            .Select(p => p.UserId)
            .ToHashSet();

        // Kaydı olmayanlar veya Enabled=true olanlar listeye dahil edilir.
        return idList
            .Where(uid => !disabledUserIds.Contains(uid))
            .ToList();
    }

    // -----------------------------------------------------------------------
    // Yardımcılar
    // -----------------------------------------------------------------------

    private Guid RequireCurrentUserId()
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            throw new ForbiddenAppException("Bu işlem için giriş yapmanız gerekmektedir.");
        return currentUser.UserId.Value;
    }
}
