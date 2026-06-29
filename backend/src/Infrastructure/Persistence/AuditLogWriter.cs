using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Infrastructure.Persistence;

/// <summary>
/// <see cref="IAuditLogWriter"/> Infrastructure implementasyonu.
/// AppDbContext üzerinden AuditLog satırını doğrudan yazar.
/// Application katmanı bu sınıfı bilmez; DI ile çözümlenir.
/// </summary>
public sealed class AuditLogWriter(AppDbContext db) : IAuditLogWriter
{
    public async Task WriteAsync(
        string entityName,
        string entityId,
        AuditAction action,
        Guid? actorUserId,
        string? actorUserName,
        string? changes,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditLog(
            entityName,
            entityId,
            action,
            actorUserId,
            actorUserName,
            DateTime.UtcNow,
            changes);

        db.AuditLogs.Add(entry);

        // AuditLog, SaveChanges döngüsünden bağımsız olarak kendi transaction'ında yazılır.
        await db.SaveChangesAsync(cancellationToken);
    }
}
