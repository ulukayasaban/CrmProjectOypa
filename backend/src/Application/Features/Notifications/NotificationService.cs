using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Mappings;
using Oypa.Crm.Contracts.Notifications;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Application.Features.Notifications;

/// <summary>Per-alıcı bildirim iş mantığı. Tüm mutasyonlar IUnitOfWork üzerinden atomik olarak kaydedilir.</summary>
public sealed class NotificationService(
    IRepository<Notification> notifications,
    ICurrentUser currentUser,
    IOrgScopeService orgScope,
    IRealtimeNotifier realtimeNotifier,
    IUnitOfWork unitOfWork) : INotificationService
{
    // -----------------------------------------------------------------------
    // Sorgular
    // -----------------------------------------------------------------------

    public async Task<IReadOnlyList<NotificationDto>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();

        var list = await notifications.ListAsync(
            n => n.RecipientUserId == userId,
            cancellationToken);

        return list
            .OrderByDescending(n => n.CreatedAtUtc)
            .Select(n => n.ToDto())
            .ToList();
    }

    public async Task<int> GetMyUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();
        return await notifications.CountAsync(
            n => n.RecipientUserId == userId && !n.IsRead,
            cancellationToken);
    }

    // -----------------------------------------------------------------------
    // Mutasyonlar — okundu işaretleme
    // -----------------------------------------------------------------------

    public async Task MarkReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();

        // Takipli (tracked) çek; alıcı mevcut kullanıcıyla eşleşmeli
        var notification = await notifications.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Bildirim", id);

        if (notification.RecipientUserId != userId)
            throw NotFoundException.For("Bildirim", id); // Başkasının bildirimini 404 ile gizle

        notification.MarkRead();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllMineReadAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();

        var unread = await notifications.ListAsync(
            n => n.RecipientUserId == userId && !n.IsRead,
            cancellationToken);

        if (unread.Count == 0)
            return;

        // ListAsync AsNoTracking döndürür; değişikliğin kalıcı olması için Update ile takibe al.
        foreach (var n in unread)
        {
            n.MarkRead();
            notifications.Update(n);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // -----------------------------------------------------------------------
    // Manuel gönderim
    // -----------------------------------------------------------------------

    public async Task SendToUnitAsync(SendNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var senderId = RequireCurrentUserId();

        // Yetki: Admin veya yönetim kapsamı olan kullanıcı (astı var)
        await EnsureCanSendAsync(cancellationToken);

        // Hedef birim alt-ağacındaki hesaplı kullanıcıları çöz; göndereni hariç tut
        var recipientUserIds = (await orgScope.GetSubtreeUserIdsAsync(request.TargetUnitId, cancellationToken))
            .Where(uid => uid != senderId)
            .ToList();

        if (recipientUserIds.Count == 0)
        {
            // Alıcı yok; işlemi sessizce tamamla (controller anlamlı mesaj döner)
            return;
        }

        // Gönderenin adını çöz
        var senderEmployee = await orgScope.GetByUserIdAsync(senderId, cancellationToken);
        var senderName = senderEmployee?.FullName ?? senderEmployee?.Title;

        await CreateForUsersAsync(
            recipientUserIds,
            request.Message,
            NotificationType.Manual,
            request.Title,
            link: null,
            senderId,
            senderName,
            cancellationToken);
    }

    // -----------------------------------------------------------------------
    // Dahili yardımcı — olay handler'ları ve SendToUnit tarafından kullanılır
    // -----------------------------------------------------------------------

    public async Task CreateForUsersAsync(
        IEnumerable<Guid> userIds,
        string message,
        NotificationType type,
        string? title = null,
        string? link = null,
        Guid? senderUserId = null,
        string? senderName = null,
        CancellationToken cancellationToken = default)
    {
        var idList = userIds.Distinct().ToList();
        if (idList.Count == 0)
            return;

        var created = new List<Notification>(idList.Count);
        foreach (var recipientId in idList)
        {
            var n = new Notification(recipientId, message, type, title, senderUserId, senderName, link);
            await notifications.AddAsync(n, cancellationToken);
            created.Add(n);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Gerçek zamanlı push — başarısız olursa loglama değil: çan polling fallback'i var
        var dtos = created.Select(n => n.ToDto()).ToList();
        foreach (var dto in dtos)
        {
            var recipientId = created.First(n => n.Id == dto.Id).RecipientUserId;
            await realtimeNotifier.NotifyUsersAsync([recipientId], dto, cancellationToken);
        }
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

    /// <summary>
    /// Admin veya yönetim kapsamı genişliği > 1 (en az bir ast) olan kullanıcıya izin verir.
    /// Yalnızca kendisi olan Sales kullanıcıları ForbiddenAppException alır.
    /// </summary>
    private async Task EnsureCanSendAsync(CancellationToken cancellationToken)
    {
        if (currentUser.Roles.Contains("Admin"))
            return;

        var scope = await orgScope.ResolveAsync(cancellationToken);

        // AllEmployees=true → kök yönetici; Ids.Count>1 → en az bir ast var
        if (scope.AllEmployees || scope.Ids.Count > 1)
            return;

        throw new ForbiddenAppException("Bildirim gönderme yetkiniz yok. Yalnızca yöneticiler ve adminler gönderebilir.");
    }
}
