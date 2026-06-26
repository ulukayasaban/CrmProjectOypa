using System.Linq.Expressions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Common.Models;
using Oypa.Crm.Application.Common.Options;
using Oypa.Crm.Application.Features.Auth;
using Oypa.Crm.Contracts.Auth;
using Oypa.Crm.Domain.Entities;
using Shouldly;

namespace Oypa.Crm.UnitTests.Features.Auth;

/// <summary>
/// change-password, forgot-password, reset-password ve update-profile
/// AuthService metotlarını IIdentityService mock'u üzerinden doğrular.
/// </summary>
public sealed class AccountManagementServiceTests
{
    private readonly IIdentityService _identity = Substitute.For<IIdentityService>();
    private readonly IJwtTokenService _jwt = Substitute.For<IJwtTokenService>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IRepository<Employee> _employees = Substitute.For<IRepository<Employee>>();
    private readonly IRepository<SalesRep> _salesReps = Substitute.For<IRepository<SalesRep>>();
    private readonly IOptions<JwtOptions> _jwtOptions =
        Options.Create(new JwtOptions { RefreshTokenDays = 7, AccessTokenMinutes = 15 });
    private readonly IOptions<AppOptions> _appOptions =
        Options.Create(new AppOptions { FrontendBaseUrl = "http://localhost:5173" });

    private readonly Guid _userId = Guid.NewGuid();

    private AuthService CreateSut() =>
        new(_identity, _jwt, _refreshTokens, _unitOfWork, _currentUser, _clock, _jwtOptions, _emailSender, _appOptions, _employees, _salesReps);

    public AccountManagementServiceTests()
    {
        _clock.UtcNow.Returns(new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc));
        // Kimliği doğrulanmış kullanıcı: _userId
        _currentUser.UserId.Returns(_userId);

        // Varsayılan: boş liste döndür
        _employees.ListAsync(Arg.Any<Expression<Func<Employee, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Employee>());
        _salesReps.ListAsync(Arg.Any<Expression<Func<SalesRep, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SalesRep>());
    }

    // ---- change-password ----

    [Fact]
    public async Task ChangePasswordAsync_CorrectCurrentPassword_Succeeds()
    {
        _identity.ChangePasswordAsync(_userId, "OldPass!1", "NewPass!2", Arg.Any<CancellationToken>())
            .Returns((true, (IReadOnlyList<string>)[]));
        var sut = CreateSut();

        // İstisna fırlatılmamalı.
        await sut.ChangePasswordAsync(new ChangePasswordRequest("OldPass!1", "NewPass!2"));
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_ThrowsConflict()
    {
        _identity.ChangePasswordAsync(_userId, "WrongPass", "NewPass!2", Arg.Any<CancellationToken>())
            .Returns((false, (IReadOnlyList<string>)["Mevcut parola yanlış."]));
        var sut = CreateSut();

        // Yanlış mevcut parola → ConflictException (HTTP 400 olarak işlenir).
        var ex = await Should.ThrowAsync<ConflictException>(
            () => sut.ChangePasswordAsync(new ChangePasswordRequest("WrongPass", "NewPass!2")));

        ex.Message.ShouldContain("Mevcut parola yanlış.");
    }

    [Fact]
    public async Task ChangePasswordAsync_NoSession_ThrowsUnauthorized()
    {
        // Oturum yoksa UserId null döner.
        _currentUser.UserId.Returns((Guid?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<UnauthorizedAppException>(
            () => sut.ChangePasswordAsync(new ChangePasswordRequest("x", "y")));
    }

    // ---- forgot-password ----

    [Fact]
    public async Task ForgotPasswordAsync_ExistingUser_CallsEmailSenderWithResetLink()
    {
        const string email = "user@oypa.com.tr";
        const string resetToken = "generated-token-abc123";

        _identity.GeneratePasswordResetTokenAsync(email, Arg.Any<CancellationToken>())
            .Returns((email, resetToken));
        var sut = CreateSut();

        await sut.ForgotPasswordAsync(new ForgotPasswordRequest(email));

        // IEmailSender mutlaka çağrılmış olmalı; gövde reset linkini içermeli.
        await _emailSender.Received(1).SendAsync(
            email,
            Arg.Any<string>(),
            Arg.Is<string>(body => body.Contains("reset-password") && body.Contains(Uri.EscapeDataString(resetToken))),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ForgotPasswordAsync_NonExistentUser_Returns200WithoutCallingEmailSender()
    {
        // Kullanıcı bulunamadı → null çifti döner.
        _identity.GeneratePasswordResetTokenAsync("unknown@oypa.com.tr", Arg.Any<CancellationToken>())
            .Returns((null, null));
        var sut = CreateSut();

        // İstisna fırlatılmamalı (varlık sızdırma engeli).
        await sut.ForgotPasswordAsync(new ForgotPasswordRequest("unknown@oypa.com.tr"));

        // E-posta gönderilmemiş olmalı.
        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ---- reset-password ----

    [Fact]
    public async Task ResetPasswordWithTokenAsync_ValidToken_Succeeds()
    {
        _identity.ResetPasswordWithTokenAsync("user@oypa.com.tr", "valid-token", "NewPass!1", Arg.Any<CancellationToken>())
            .Returns((true, (IReadOnlyList<string>)[]));
        var sut = CreateSut();

        // İstisna fırlatılmamalı.
        await sut.ResetPasswordWithTokenAsync(
            new ResetPasswordRequest("user@oypa.com.tr", "valid-token", "NewPass!1"));
    }

    [Fact]
    public async Task ResetPasswordWithTokenAsync_InvalidToken_ThrowsConflict()
    {
        _identity.ResetPasswordWithTokenAsync("user@oypa.com.tr", "bad-token", "NewPass!1", Arg.Any<CancellationToken>())
            .Returns((false, (IReadOnlyList<string>)["Geçersiz parola sıfırlama jetonu."]));
        var sut = CreateSut();

        // Geçersiz token → ConflictException (HTTP 400 olarak işlenir).
        var ex = await Should.ThrowAsync<ConflictException>(
            () => sut.ResetPasswordWithTokenAsync(
                new ResetPasswordRequest("user@oypa.com.tr", "bad-token", "NewPass!1")));

        ex.Message.ShouldContain("Geçersiz parola sıfırlama jetonu.");
    }

    // ---- update profile ----

    [Fact]
    public async Task UpdateProfileAsync_ValidRequest_ReturnsUpdatedUserDto()
    {
        var updatedInfo = new AuthUserInfo(
            _userId, "user@oypa.com.tr", "Yeni Ad Soyad", "Yönetici", "0532 111 22 33", ["Admin"]);

        _identity.UpdateProfileAsync(
            _userId, "Yeni Ad Soyad", "0532 111 22 33", "Yönetici", Arg.Any<CancellationToken>())
            .Returns(updatedInfo);
        var sut = CreateSut();

        var result = await sut.UpdateProfileAsync(
            new UpdateProfileRequest("Yeni Ad Soyad", "0532 111 22 33", "Yönetici"));

        result.FullName.ShouldBe("Yeni Ad Soyad");
        result.Phone.ShouldBe("0532 111 22 33");
        result.Position.ShouldBe("Yönetici");
        result.Id.ShouldBe(_userId);
    }

    [Fact]
    public async Task UpdateProfileAsync_NoSession_ThrowsUnauthorized()
    {
        _currentUser.UserId.Returns((Guid?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<UnauthorizedAppException>(
            () => sut.UpdateProfileAsync(new UpdateProfileRequest("Ad", null, null)));
    }

    [Fact]
    public async Task UpdateProfileAsync_NullOptionalFields_UpdatesOnlyFullName()
    {
        var updatedInfo = new AuthUserInfo(
            _userId, "user@oypa.com.tr", "Sadece İsim", null, null, ["Sales"]);

        _identity.UpdateProfileAsync(
            _userId, "Sadece İsim", null, null, Arg.Any<CancellationToken>())
            .Returns(updatedInfo);
        var sut = CreateSut();

        var result = await sut.UpdateProfileAsync(new UpdateProfileRequest("Sadece İsim", null, null));

        result.FullName.ShouldBe("Sadece İsim");
        result.Phone.ShouldBeNull();
        result.Position.ShouldBeNull();
    }
}
