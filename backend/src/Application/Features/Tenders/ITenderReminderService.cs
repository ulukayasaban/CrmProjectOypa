namespace Oypa.Crm.Application.Features.Tenders;

/// <summary>Yaklaşan ihale bildirim tarama servisi. Test edilebilirlik için Application katmanında tanımlanır.</summary>
public interface ITenderReminderService
{
    /// <summary>
    /// TenderDate'e <paramref name="daysAhead"/> gün veya daha az kalan, henüz bildirilmemiş ve
    /// atanmış bir sorumlusu olan aktif ihaleler için bildirim gönderir.
    /// </summary>
    /// <returns>İşlenen (bildirim gönderilen) ihale sayısı.</returns>
    Task<int> NotifyApproachingAsync(int daysAhead, CancellationToken cancellationToken = default);
}
