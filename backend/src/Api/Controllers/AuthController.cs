using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Oypa.Crm.Api.Extensions;
using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Common.Options;
using Oypa.Crm.Application.Features.Auth;
using Oypa.Crm.Contracts.Auth;
using Oypa.Crm.Contracts.Common;
using Oypa.Crm.Contracts.Employees;

namespace Oypa.Crm.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IAuthService authService,
    IIdentityService identityService,
    IDateTimeProvider clock,
    IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    /// <summary>
    /// Refresh token'ın taşındığı HttpOnly çerez adı. localStorage yerine çerez
    /// kullanılması XSS ile token sızdırılmasını engeller (JS erişemez).
    /// </summary>
    private const string RefreshCookieName = "oypa_rt";

    /// <summary>Çerez yalnızca auth uçlarına gönderilsin diye dar path.</summary>
    private const string RefreshCookiePath = "/api/auth";

    private readonly JwtOptions _jwt = jwtOptions.Value;

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.AuthLogin)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, ClientIp(), cancellationToken);
        SetRefreshCookie(result.RefreshToken);
        return Ok(ApiResponse<AuthResponse>.Ok(WithoutRefreshToken(result), "Giriş başarılı."));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.AuthRefresh)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        // Refresh token artık istek gövdesinde değil, HttpOnly çerezde taşınır.
        var refreshToken = Request.Cookies[RefreshCookieName];
        if (string.IsNullOrEmpty(refreshToken))
            throw new UnauthorizedAppException("Refresh token bulunamadı.");

        var result = await authService.RefreshAsync(refreshToken, ClientIp(), cancellationToken);
        SetRefreshCookie(result.RefreshToken);
        return Ok(ApiResponse<AuthResponse>.Ok(WithoutRefreshToken(result), "Token yenilendi."));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshCookieName];
        if (!string.IsNullOrEmpty(refreshToken))
            await authService.LogoutAsync(refreshToken, cancellationToken);

        ClearRefreshCookie();
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
    /// Silme işlemi audit'e kaydedilir.
    /// </summary>
    [HttpDelete("users/{id:guid}")]
    [Authorize(AuthenticationExtensions.AdminPolicy)]
    [EnableRateLimiting(RateLimitingExtensions.AdminSensitive)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        await authService.DeleteUserAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Kullanıcı silindi."));
    }

    /// <summary>
    /// Belirtilen kullanıcının rolünü değiştirir. Yalnızca Admin erişebilir.
    /// Kendi rolünü değiştirmeye çalışmak 403 döner (kendini kilitlenmeyi önler).
    /// </summary>
    [HttpPut("users/{id:guid}/role")]
    [Authorize(AuthenticationExtensions.AdminPolicy)]
    [EnableRateLimiting(RateLimitingExtensions.AdminSensitive)]
    public async Task<IActionResult> ChangeUserRole(Guid id, ChangeUserRoleRequest request, CancellationToken cancellationToken)
    {
        await authService.ChangeUserRoleAsync(id, request.Role, cancellationToken);
        return Ok(ApiResponse.Ok("Kullanıcı rolü güncellendi."));
    }

    /// <summary>
    /// Belirtilen kullanıcının parolasını sıfırlar ve geçici parolayı döndürür.
    /// Yalnızca Admin erişebilir.
    /// </summary>
    [HttpPost("users/{id:guid}/reset-password")]
    [Authorize(AuthenticationExtensions.AdminPolicy)]
    [EnableRateLimiting(RateLimitingExtensions.AdminSensitive)]
    public async Task<IActionResult> ResetUserPassword(Guid id, CancellationToken cancellationToken)
    {
        var credentials = await authService.ResetUserPasswordAsync(id, cancellationToken);
        return Ok(ApiResponse<AccountCredentialDto>.Ok(credentials, "Parola sıfırlandı."));
    }

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    /// <summary>
    /// Refresh token'ı HttpOnly çereze yazar. Güvenlik bayrakları isteğin gerçek
    /// şemasına göre belirlenir (ortam bayrağına değil) — bkz. BuildCookieOptions.
    /// </summary>
    private void SetRefreshCookie(string token)
    {
        Response.Cookies.Append(RefreshCookieName, token, BuildCookieOptions(
            clock.UtcNow.AddDays(_jwt.RefreshTokenDays)));
    }

    private void ClearRefreshCookie()
    {
        // Silmek için aynı Path/Secure/SameSite ile geçmiş tarihli çerez gönderilir.
        Response.Cookies.Append(RefreshCookieName, string.Empty, BuildCookieOptions(
            clock.UtcNow.AddDays(-1)));
    }

    private CookieOptions BuildCookieOptions(DateTimeOffset expires)
    {
        // Çerez güvenliği, ASPNETCORE_ENVIRONMENT bayrağına değil isteğin GERÇEK
        // şemasına göre belirlenir → hem http hem https aynı kodla doğru çalışır:
        //   - HTTP  → Secure=false + SameSite=Lax  (http sunucu/lokal testte çerez gider)
        //   - HTTPS → Secure=true  + SameSite=None (production + çapraz alan adı senaryosu)
        // Not: TLS'i ters proxy sonlandırıyorsa Request.IsHttps'in doğru gelmesi için
        // ForwardedHeaders middleware (X-Forwarded-Proto) etkin olmalı.
        var secure = Request.IsHttps;
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = secure ? SameSiteMode.None : SameSiteMode.Lax,
            Path = RefreshCookiePath,
            Expires = expires,
            IsEssential = true,
        };
    }

    /// <summary>Gövdede refresh token sızdırmamak için boşaltılmış kopya döner.</summary>
    private static AuthResponse WithoutRefreshToken(AuthResponse response) =>
        response with { RefreshToken = string.Empty };
}
