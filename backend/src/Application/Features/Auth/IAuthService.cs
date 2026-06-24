using Oypa.Crm.Contracts.Auth;

namespace Oypa.Crm.Application.Features.Auth;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    Task<AuthResponse> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default);

    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<Guid> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);

    Task<UserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    /// <summary>Kimliği doğrulanmış kullanıcının parolasını değiştirir.</summary>
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parola sıfırlama bağlantısı e-postası gönderir.
    /// Kullanıcı bulunamasa bile 200 döner (varlık sızdırma engeli).
    /// </summary>
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>E-posta + token ile parola sıfırlama işlemini tamamlar.</summary>
    Task ResetPasswordWithTokenAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>Kimliği doğrulanmış kullanıcının profil bilgilerini günceller.</summary>
    Task<UserDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);
}
