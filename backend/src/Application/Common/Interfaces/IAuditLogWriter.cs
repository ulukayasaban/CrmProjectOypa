using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Application.Common.Interfaces;

/// <summary>
/// Explicit (açık) AuditLog kaydı oluşturma sözleşmesi.
/// AuditSaveChangesInterceptor yalnızca BaseEntity türevlerini kapsar;
/// ApplicationUser (IdentityUser) gibi dışarıda kalan entity'ler için
/// servis katmanından çağrılır.
/// </summary>
public interface IAuditLogWriter
{
    /// <summary>
    /// Belirtilen entity için tek bir AuditLog satırı yazar ve kalıcılaştırır.
    /// </summary>
    Task WriteAsync(
        string entityName,
        string entityId,
        AuditAction action,
        Guid? actorUserId,
        string? actorUserName,
        string? changes,
        CancellationToken cancellationToken = default);
}
