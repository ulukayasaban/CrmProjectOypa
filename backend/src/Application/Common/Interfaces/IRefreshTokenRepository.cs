using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Application.Common.Interfaces;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// <paramref name="olderThanUtc"/> tarihinden önce süresi dolmuş token'ları toplu siler.
    /// Arka plan temizleme işlemi için tasarlanmıştır; geri dönüş değeri silinen kayıt sayısıdır.
    /// </summary>
    Task<int> DeleteExpiredAsync(DateTime olderThanUtc, CancellationToken cancellationToken = default);
}
