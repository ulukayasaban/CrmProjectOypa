using Oypa.Crm.Application.Common.Models;

namespace Oypa.Crm.Application.Common.Interfaces;

/// <summary>ASP.NET Identity altyapısını Application katmanından soyutlar.</summary>
public interface IIdentityService
{
    Task<AuthUserInfo?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<AuthUserInfo?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<CreateUserResult> CreateUserAsync(
        string email,
        string password,
        string fullName,
        string role,
        CancellationToken cancellationToken = default);

    /// <summary>Kullanıcının tüm rollerini kaldırır ve yeni rolü atar (Admin veya Sales).</summary>
    Task SetRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);

    /// <summary>Politika-uyumlu rastgele geçici parola üretir, mevcut parolayı değiştirir ve yeni parolayı döndürür.</summary>
    Task<string> ResetPasswordAsync(Guid userId, CancellationToken cancellationToken = default);
}
