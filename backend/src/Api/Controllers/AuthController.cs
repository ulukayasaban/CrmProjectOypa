using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Oypa.Crm.Api.Extensions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Features.Auth;
using Oypa.Crm.Contracts.Auth;
using Oypa.Crm.Contracts.Common;

namespace Oypa.Crm.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IAuthService authService,
    IIdentityService identityService,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.AuthLogin)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, ClientIp(), cancellationToken);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Giriş başarılı."));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.AuthRefresh)]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RefreshAsync(request.RefreshToken, ClientIp(), cancellationToken);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Token yenilendi."));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request.RefreshToken, cancellationToken);
        return Ok(ApiResponse.Ok("Çıkış yapıldı."));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var user = await authService.GetCurrentUserAsync(cancellationToken);
        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    [HttpPost("register")]
    [Authorize(AuthenticationExtensions.AdminPolicy)]
    [EnableRateLimiting(RateLimitingExtensions.AdminSensitive)]
    public async Task<IActionResult> Register(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var userId = await authService.RegisterAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<Guid>.Ok(userId, "Kullanıcı oluşturuldu."));
    }

    /// <summary>
    /// Kimliği doğrulanmış kullanıcının parolasını değiştirir.
    /// Mevcut parola yanlışsa 400 döner.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [EnableRateLimiting(RateLimitingExtensions.AdminSensitive)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ChangePasswordAsync(request, cancellationToken);
        return Ok(ApiResponse.Ok("Parola başarıyla değiştirildi."));
    }

    /// <summary>
    /// Parola sıfırlama bağlantısı gönderir.
    /// Kullanıcı bulunamasa bile 200 döner (e-posta varlığı sızdırılmaz).
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.AuthLogin)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ForgotPasswordAsync(request, cancellationToken);
        return Ok(ApiResponse.Ok("Parola sıfırlama bağlantısı e-posta adresinize gönderildi."));
    }

    /// <summary>
    /// E-posta ve token ile parola sıfırlama işlemini tamamlar.
    /// Geçersiz token veya parola → 400.
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.AuthLogin)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ResetPasswordWithTokenAsync(request, cancellationToken);
        return Ok(ApiResponse.Ok("Parola başarıyla sıfırlandı."));
    }

    /// <summary>
    /// Kimliği doğrulanmış kullanıcının profil bilgilerini günceller.
    /// Güncellenmiş UserDto döner.
    /// </summary>
    [HttpPatch("me")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var updated = await authService.UpdateProfileAsync(request, cancellationToken);
        return Ok(ApiResponse<UserDto>.Ok(updated, "Profil güncellendi."));
    }

    /// <summary>
    /// Tüm Identity kullanıcılarını rolleriyle listeler. Yalnızca Admin erişebilir.
    /// </summary>
    [HttpGet("users")]
    [Authorize(AuthenticationExtensions.AdminPolicy)]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var users = await identityService.ListUsersAsync(cancellationToken);
        // AuthUserInfo → UserDto dönüşümü; entity asla doğrudan dönülmez.
        var dtos = users.Select(u => new UserDto(u.Id, u.Email, u.FullName, u.Position, u.Phone, u.Roles)).ToList();
        return Ok(ApiResponse<IReadOnlyList<UserDto>>.Ok(dtos));
    }

    /// <summary>
    /// Belirtilen kullanıcıyı siler. Kendini silmek 403 döner.
    /// İlişkili Employee kaydı varsa ApplicationUserId null'a düşer (Employee silinmez).
    /// </summary>
    [HttpDelete("users/{id:guid}")]
    [Authorize(AuthenticationExtensions.AdminPolicy)]
    [EnableRateLimiting(RateLimitingExtensions.AdminSensitive)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var actorId = currentUser.UserId
            ?? throw new Application.Common.Exceptions.UnauthorizedAppException("Oturum bulunamadı.");

        await identityService.DeleteUserAsync(id, actorId, cancellationToken);
        return Ok(ApiResponse.Ok("Kullanıcı silindi."));
    }

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
