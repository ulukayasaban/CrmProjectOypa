using Microsoft.Extensions.Options;
using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Common.Models;
using Oypa.Crm.Application.Common.Options;
using Oypa.Crm.Contracts.Auth;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Application.Features.Auth;

public sealed class AuthService(
    IIdentityService identityService,
    IJwtTokenService jwtTokenService,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var user = await identityService.ValidateCredentialsAsync(request.Email, request.Password, cancellationToken)
                   ?? throw new UnauthorizedAppException("E-posta veya parola hatalı.");

        return await IssueTokensAsync(user, ipAddress, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var hash = jwtTokenService.HashToken(refreshToken);
        var existing = await refreshTokens.GetByHashAsync(hash, cancellationToken)
                       ?? throw new UnauthorizedAppException("Geçersiz refresh token.");

        // Yeniden kullanım tespiti: iptal edilmiş bir token tekrar kullanıldıysa
        // kullanıcının tüm aktif oturumları sonlandırılır.
        if (existing.IsRevoked)
        {
            await RevokeAllActiveAsync(existing.UserId, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAppException("Refresh token yeniden kullanıldı; oturum güvenlik nedeniyle sonlandırıldı.");
        }

        if (!existing.IsActiveAt(clock.UtcNow))
            throw new UnauthorizedAppException("Refresh token süresi dolmuş.");

        var user = await identityService.GetByIdAsync(existing.UserId, cancellationToken)
                   ?? throw new UnauthorizedAppException("Kullanıcı bulunamadı.");

        // Rotasyon: eski token iptal edilir, yenisi üretilir.
        var rawNew = jwtTokenService.GenerateRefreshToken();
        var hashNew = jwtTokenService.HashToken(rawNew);
        existing.Revoke(hashNew);

        var newToken = new RefreshToken(user.Id, hashNew, clock.UtcNow.AddDays(_jwt.RefreshTokenDays), ipAddress);
        await refreshTokens.AddAsync(newToken, cancellationToken);

        var access = jwtTokenService.CreateAccessToken(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(access.Token, access.ExpiresAtUtc, rawNew, ToUserDto(user));
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var hash = jwtTokenService.HashToken(refreshToken);
        var existing = await refreshTokens.GetByHashAsync(hash, cancellationToken);
        if (existing is not null && existing.IsActiveAt(clock.UtcNow))
        {
            existing.Revoke();
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<Guid> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        var result = await identityService.CreateUserAsync(
            request.Email, request.Password, request.FullName, request.Role, cancellationToken);

        if (!result.Succeeded || result.UserId is null)
            throw new ConflictException(string.Join(" ", result.Errors.DefaultIfEmpty("Kullanıcı oluşturulamadı.")));

        return result.UserId.Value;
    }

    public async Task<UserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAppException("Oturum bulunamadı.");
        var user = await identityService.GetByIdAsync(userId, cancellationToken)
                   ?? throw new UnauthorizedAppException("Kullanıcı bulunamadı.");
        return ToUserDto(user);
    }

    private async Task<AuthResponse> IssueTokensAsync(AuthUserInfo user, string? ipAddress, CancellationToken cancellationToken)
    {
        var access = jwtTokenService.CreateAccessToken(user);
        var rawRefresh = jwtTokenService.GenerateRefreshToken();
        var hash = jwtTokenService.HashToken(rawRefresh);

        var entity = new RefreshToken(user.Id, hash, clock.UtcNow.AddDays(_jwt.RefreshTokenDays), ipAddress);
        await refreshTokens.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(access.Token, access.ExpiresAtUtc, rawRefresh, ToUserDto(user));
    }

    private async Task RevokeAllActiveAsync(Guid userId, CancellationToken cancellationToken)
    {
        var active = await refreshTokens.GetActiveByUserAsync(userId, cancellationToken);
        foreach (var token in active)
            token.Revoke();
    }

    private static UserDto ToUserDto(AuthUserInfo user) =>
        new(user.Id, user.Email, user.FullName, user.Position, user.Phone, user.Roles);
}
