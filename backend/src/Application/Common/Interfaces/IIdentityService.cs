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

    /// <summary>
    /// Kimliği doğrulanmış kullanıcının mevcut parolasını doğrulayarak yeni parolayla değiştirir.
    /// Başarısızsa hata mesajları döndürülür.
    /// </summary>
    Task<(bool Succeeded, IReadOnlyList<string> Errors)> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirtilen e-postaya ait kullanıcı için parola sıfırlama jetonu üretir.
    /// Kullanıcı bulunamazsa null döndürür (varlık sızdırma engeli için null dönüşünün kontrol edilmesi gerekir).
    /// </summary>
    Task<(string? Email, string? Token)> GeneratePasswordResetTokenAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// E-posta + token çifti ile parolayı sıfırlar.
    /// Geçersiz token veya parola politikası ihlali durumunda hata mesajları döner.
    /// </summary>
    Task<(bool Succeeded, IReadOnlyList<string> Errors)> ResetPasswordWithTokenAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcı profil alanlarını (FullName, Phone, Position) günceller ve
    /// güncellenmiş kullanıcı bilgisini döndürür.
    /// </summary>
    Task<AuthUserInfo> UpdateProfileAsync(
        Guid userId,
        string fullName,
        string? phone,
        string? position,
        CancellationToken cancellationToken = default);

    /// <summary>Tüm Identity kullanıcılarını rolleriyle birlikte listeler. Yalnızca Admin kullanabilir.</summary>
    Task<IReadOnlyList<AuthUserInfo>> ListUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirtilen kullanıcıyı siler.
    /// currentUserId ile eşleşirse ForbiddenAppException fırlatır (kendini silme engeli).
    /// </summary>
    Task DeleteUserAsync(Guid userId, Guid currentUserId, CancellationToken cancellationToken = default);
}
