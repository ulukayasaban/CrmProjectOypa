using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Domain.Common;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core SaveChanges intercept ederek <see cref="AuditLog"/> kayıtları oluşturur.
/// Added → <see cref="AuditAction.Created"/>,
/// Modified → <see cref="AuditAction.Updated"/> (soft-delete durumunda <see cref="AuditAction.Deleted"/>),
/// Deleted → <see cref="AuditAction.Deleted"/>.
/// AuditLog kendisi ve RefreshToken (teknik) audit dışı tutulur (sonsuz döngü/gürültü önlenir).
/// </summary>
public sealed class AuditSaveChangesInterceptor(ICurrentUser currentUser) : SaveChangesInterceptor
{
    // Audit'lenmeyen türler: kendisi ve düşük değerli teknik varlıklar
    private static readonly HashSet<Type> ExcludedTypes =
    [
        typeof(AuditLog),
        typeof(RefreshToken)
    ];

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            AddAuditLogs(eventData.Context);

        return new ValueTask<InterceptionResult<int>>(result);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            AddAuditLogs(eventData.Context);

        return result;
    }

    // ────────────────────────────────────────────────
    // Özel metotlar
    // ────────────────────────────────────────────────

    private void AddAuditLogs(DbContext context)
    {
        var now = DateTime.UtcNow;
        var userId = currentUser.UserId;
        var userName = currentUser.Email;

        // ChangeTracker'dan audit'lenecek entry'leri seç
        var entries = context.ChangeTracker.Entries<BaseEntity>()
            .Where(e =>
                e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
                !ExcludedTypes.Contains(e.Entity.GetType()))
            .ToList();

        var auditLogs = new List<AuditLog>(entries.Count);

        foreach (var entry in entries)
        {
            var entityName = entry.Entity.GetType().Name;
            var entityId = entry.Entity.Id.ToString();

            // Soft-delete tespiti: Modified durumunda IsDeleted=true ise Deleted aksiyonu yaz
            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Created,
                EntityState.Deleted => AuditAction.Deleted,
                EntityState.Modified when IsSoftDeleted(entry) => AuditAction.Deleted,
                _ => AuditAction.Updated
            };

            // Değişen alanların kısa özetini oluştur
            var changes = BuildChanges(entry, action);

            auditLogs.Add(new AuditLog(entityName, entityId, action, userId, userName, now, changes));
        }

        if (auditLogs.Count > 0)
            context.Set<AuditLog>().AddRange(auditLogs);
    }

    /// <summary>Entry'nin soft-delete olarak işaretlenip işaretlenmediğini kontrol eder.</summary>
    private static bool IsSoftDeleted(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var isDeletedProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == nameof(ISoftDelete.IsDeleted));
        return isDeletedProp?.CurrentValue is true;
    }

    /// <summary>
    /// Değişen özellik adlarını ve yeni değerlerini JSON olarak döndürür.
    /// Hassas alanlar (parola, token) hariç; büyük payload 1000 karakterde kırpılır.
    /// </summary>
    private static string? BuildChanges(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry,
        AuditAction action)
    {
        try
        {
            // Oluşturma durumunda sadece "Created" yaz (tüm alanlar zaten yeni)
            if (action == AuditAction.Created)
                return null;

            // Güncelleme/silme: değişen alanları topla
            var changedProps = entry.Properties
                .Where(p =>
                    p.IsModified &&
                    !IsSensitiveProperty(p.Metadata.Name))
                .Select(p => new
                {
                    Field = p.Metadata.Name,
                    Old = FormatValue(p.OriginalValue),
                    New = FormatValue(p.CurrentValue)
                })
                .ToList();

            if (changedProps.Count == 0)
                return null;

            var json = JsonSerializer.Serialize(changedProps);

            // 2000 karakteri aşarsa kırp
            return json.Length > 2000 ? json[..2000] + "…" : json;
        }
        catch
        {
            // Audit log üretimi hiçbir zaman ana işlemi patlatmamalı
            return null;
        }
    }

    private static bool IsSensitiveProperty(string name) =>
        name is "PasswordHash" or "SecurityStamp" or "ConcurrencyStamp"
            or "TokenHash" or "NormalizedEmail" or "NormalizedUserName";

    private static string? FormatValue(object? value) =>
        value switch
        {
            null => null,
            string s => s.Length > 200 ? s[..200] + "…" : s,
            _ => value.ToString()
        };
}
