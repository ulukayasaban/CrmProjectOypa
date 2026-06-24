using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Oypa.Crm.Api.Extensions;
using Oypa.Crm.Application.Features.Notifications;
using Oypa.Crm.Contracts.Common;
using Oypa.Crm.Contracts.Notifications;

namespace Oypa.Crm.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController(INotificationService notificationService) : ControllerBase
{
    /// <summary>Geçerli kullanıcının bildirimlerini tarihe göre azalan sırada döndürür.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var data = await notificationService.GetMineAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<NotificationDto>>.Ok(data));
    }

    /// <summary>Geçerli kullanıcının okunmamış bildirim sayısını döndürür.</summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var count = await notificationService.GetMyUnreadCountAsync(cancellationToken);
        return Ok(ApiResponse<int>.Ok(count));
    }

    /// <summary>Belirtilen bildirimi okundu olarak işaretler (yalnız kendi bildirimini).</summary>
    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await notificationService.MarkReadAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Bildirim okundu olarak işaretlendi."));
    }

    /// <summary>Geçerli kullanıcının tüm okunmamış bildirimlerini okundu olarak işaretler.</summary>
    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await notificationService.MarkAllMineReadAsync(cancellationToken);
        return Ok(ApiResponse.Ok("Tüm bildirimler okundu olarak işaretlendi."));
    }

    /// <summary>
    /// Hedef birimin alt-ağacındaki kullanıcılara manuel bildirim gönderir.
    /// Yetki: Admin veya astı olan yönetici.
    /// </summary>
    [HttpPost("send")]
    [EnableRateLimiting(RateLimitingExtensions.AdminSensitive)]
    public async Task<IActionResult> Send(
        [FromBody] SendNotificationRequest request,
        CancellationToken cancellationToken)
    {
        await notificationService.SendToUnitAsync(request, cancellationToken);
        return Ok(ApiResponse.Ok("Bildirim gönderildi."));
    }

    /// <summary>
    /// Geçerli kullanıcının belirtilen bildirimini siler.
    /// Başkasının bildirimine erişim 404 döner.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await notificationService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Bildirim silindi."));
    }
}
