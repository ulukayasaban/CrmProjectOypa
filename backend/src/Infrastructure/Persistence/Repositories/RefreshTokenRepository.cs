using Microsoft.EntityFrameworkCore;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository(AppDbContext db) : Repository<RefreshToken>(db), IRefreshTokenRepository
{
    // İzlenen (tracked) sorgu: dönen token üzerinde Revoke çağrılabilsin diye.
    public async Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        await Set.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await Set
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > now)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Süresi <paramref name="olderThanUtc"/> tarihinden önce dolmuş token'ları toplu siler.
    /// EF Core ExecuteDeleteAsync ile tek SQL komutu gönderilir; change-tracker yükü olmaz.
    /// InMemory sağlayıcı ExecuteDeleteAsync'i desteklemez; bu durumda RemoveRange ile fallback yapılır.
    /// </summary>
    public async Task<int> DeleteExpiredAsync(DateTime olderThanUtc, CancellationToken cancellationToken = default)
    {
        // InMemory provider (test ortamı) ExecuteDeleteAsync'i desteklemez; provider türü kontrol edilir.
        var isRelational = Db.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == false;

        if (isRelational)
        {
            return await Set
                .Where(t => t.ExpiresAtUtc < olderThanUtc)
                .ExecuteDeleteAsync(cancellationToken);
        }

        // InMemory fallback: change-tracker üzerinden sil.
        var expired = await Set
            .Where(t => t.ExpiresAtUtc < olderThanUtc)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
            return 0;

        Set.RemoveRange(expired);
        await Db.SaveChangesAsync(cancellationToken);
        return expired.Count;
    }
}
