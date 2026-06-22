using Oypa.Crm.Domain.Common;

namespace Oypa.Crm.Domain.Entities;

/// <summary>
/// Yenileme (refresh) token kaydı. Token'ın kendisi DB'de tutulmaz;
/// yalnızca <see cref="TokenHash"/> (SHA-256) saklanır. Rotasyon ve
/// yeniden-kullanım tespiti için zincir bilgisi taşınır.
/// </summary>
public class RefreshToken : BaseEntity
{
    private RefreshToken() { }

    public RefreshToken(Guid userId, string tokenHash, DateTime expiresAtUtc, string? createdByIp)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedByIp = createdByIp;
    }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public string? CreatedByIp { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    public bool IsRevoked => RevokedAtUtc is not null;

    /// <summary>Verilen ana göre süresi dolmuş mu. Statik saat yerine enjekte
    /// edilen saatle çağrılır (test edilebilirlik + tutarlılık).</summary>
    public bool IsExpiredAt(DateTime nowUtc) => nowUtc >= ExpiresAtUtc;

    /// <summary>Verilen ana göre token aktif mi (iptal edilmemiş ve süresi dolmamış).</summary>
    public bool IsActiveAt(DateTime nowUtc) => !IsRevoked && !IsExpiredAt(nowUtc);

    /// <summary>Token'ı iptal eder; rotasyonda yerine geçen token'ın hash'ini bağlar.</summary>
    public void Revoke(string? replacedByTokenHash = null)
    {
        if (IsRevoked) return;
        RevokedAtUtc = DateTime.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
