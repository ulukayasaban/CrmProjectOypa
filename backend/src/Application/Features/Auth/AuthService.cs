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
    IOptions<JwtOptions> jwtOptions,
    IEmailSender emailSender,
    IOptions<AppOptions> appOptions,
    IRepository<Employee> employees,
    IRepository<SalesRep> salesReps) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;
    private readonly AppOptions _app = appOptions.Value;

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

        var assignedSalesRepId = await ResolveAssignedSalesRepIdAsync(userId, cancellationToken);
        return ToUserDto(user, assignedSalesRepId);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAppException("Oturum bulunamadı.");

        var (succeeded, errors) = await identityService.ChangePasswordAsync(
            userId, request.CurrentPassword, request.NewPassword, cancellationToken);

        if (!succeeded)
            // Hatalı mevcut parola veya politika ihlali: 400 ile döner.
            throw new ConflictException(string.Join(" ", errors.DefaultIfEmpty("Parola değiştirilemedi.")));
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var (email, token) = await identityService.GeneratePasswordResetTokenAsync(request.Email, cancellationToken);

        // Kullanıcı bulunamasa bile sessizce döner — varlık sızdırma koruması.
        if (email is null || token is null)
            return;

        var encodedToken = Uri.EscapeDataString(token);
        var resetLink = $"{_app.FrontendBaseUrl}/reset-password?email={Uri.EscapeDataString(email)}&token={encodedToken}";

        var htmlBody = $"""
            <p>Parolanızı sıfırlamak için aşağıdaki bağlantıya tıklayın:</p>
            <p><a href="{resetLink}">Parolayı Sıfırla</a></p>
            <p>Bu bağlantı geçici olarak geçerlidir. İsteği siz başlatmadıysanız bu e-postayı yok sayın.</p>
            """;

        await emailSender.SendAsync(email, "OYPA CRM — Parola Sıfırlama", htmlBody, cancellationToken);
    }

    public async Task ResetPasswordWithTokenAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var (succeeded, errors) = await identityService.ResetPasswordWithTokenAsync(
            request.Email, request.Token, request.NewPassword, cancellationToken);

        if (!succeeded)
            throw new ConflictException(string.Join(" ", errors.DefaultIfEmpty("Parola sıfırlanamadı.")));
    }

    public async Task<UserDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAppException("Oturum bulunamadı.");

        var updated = await identityService.UpdateProfileAsync(
            userId, request.FullName, request.Phone, request.Position, cancellationToken);

        return ToUserDto(updated);
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

    /// <summary>
    /// Kullanıcıya bağlı SalesRep kaydının Id'sini döndürür.
    /// Employee → ApplicationUserId zinciriyle çözülür; eşleşme yoksa null.
    /// </summary>
    private async Task<Guid?> ResolveAssignedSalesRepIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var employee = (await employees.ListAsync(
            e => e.ApplicationUserId == userId,
            cancellationToken)).FirstOrDefault();

        if (employee is null)
            return null;

        var rep = (await salesReps.ListAsync(
            r => r.EmployeeId == employee.Id,
            cancellationToken)).FirstOrDefault();

        return rep?.Id;
    }

    private static UserDto ToUserDto(AuthUserInfo user, Guid? assignedSalesRepId = null) =>
        new(user.Id, user.Email, user.FullName, user.Position, user.Phone, user.Roles, assignedSalesRepId);
}
