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
}
