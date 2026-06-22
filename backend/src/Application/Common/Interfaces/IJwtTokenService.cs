using Oypa.Crm.Application.Common.Models;

namespace Oypa.Crm.Application.Common.Interfaces;

/// <summary>JWT erişim token'ı ve refresh token üretimi/hash'lenmesi.</summary>
public interface IJwtTokenService
{
    TokenResult CreateAccessToken(AuthUserInfo user);

    /// <summary>Kriptografik olarak güçlü, ham (saklanmayan) refresh token üretir.</summary>
    string GenerateRefreshToken();

    /// <summary>Ham refresh token'ın DB'de saklanacak SHA-256 hash'ini üretir.</summary>
    string HashToken(string token);
}
