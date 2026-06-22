using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Oypa.Crm.Api.Extensions;
using Oypa.Crm.Application.Features.Auth;
using Oypa.Crm.Contracts.Auth;
using Oypa.Crm.Contracts.Common;

namespace Oypa.Crm.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
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

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
