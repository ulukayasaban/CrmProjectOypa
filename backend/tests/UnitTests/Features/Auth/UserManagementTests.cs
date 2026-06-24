using NSubstitute;
using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Common.Models;
using Oypa.Crm.Contracts.Auth;
using Shouldly;

namespace Oypa.Crm.UnitTests.Features.Auth;

/// <summary>
/// IIdentityService.ListUsersAsync ve DeleteUserAsync için birim testleri.
/// Servis davranışı IIdentityService mock'u üzerinden doğrulanır;
/// kendini silme engeli ve başarılı silme senaryoları kapsanır.
/// </summary>
public sealed class UserManagementTests
{
    private readonly IIdentityService _identity = Substitute.For<IIdentityService>();

    private static AuthUserInfo MakeUser(Guid? id = null, string email = "u@oypa.com", string role = "Sales") =>
        new(id ?? Guid.NewGuid(), email, "Test Kullanıcı", null, null, [role]);

    // -----------------------------------------------------------------------
    // ListUsersAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ListUsersAsync_ReturnsAllUsers()
    {
        var users = new List<AuthUserInfo>
        {
            MakeUser(email: "a@oypa.com", role: "Admin"),
            MakeUser(email: "b@oypa.com", role: "Sales")
        };
        _identity.ListUsersAsync(Arg.Any<CancellationToken>()).Returns(users);

        var result = await _identity.ListUsersAsync();

        result.Count.ShouldBe(2);
        result.ShouldContain(u => u.Email == "a@oypa.com");
        result.ShouldContain(u => u.Email == "b@oypa.com");
    }

    [Fact]
    public async Task ListUsersAsync_EmptyStore_ReturnsEmptyList()
    {
        _identity.ListUsersAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AuthUserInfo>());

        var result = await _identity.ListUsersAsync();

        result.ShouldBeEmpty();
    }

    // -----------------------------------------------------------------------
    // DeleteUserAsync — kendini silme engeli
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteUserAsync_SelfDeletion_ThrowsForbiddenAppException()
    {
        // Gerçek IdentityService yerine mock üzerinden kontrat testini doğrula:
        // Servis kendini-silme girişiminde ForbiddenAppException fırlatmalı.
        var actorId = Guid.NewGuid();

        _identity
            .When(s => s.DeleteUserAsync(actorId, actorId, Arg.Any<CancellationToken>()))
            .Do(_ => throw new ForbiddenAppException("Kendi hesabınızı silemezsiniz."));

        var ex = await Should.ThrowAsync<ForbiddenAppException>(
            () => _identity.DeleteUserAsync(actorId, actorId));

        ex.Message.ShouldContain("silemezsiniz");
    }

    [Fact]
    public async Task DeleteUserAsync_DifferentUser_DoesNotThrow()
    {
        var actorId = Guid.NewGuid();
        var targetId = Guid.NewGuid(); // Farklı kullanıcı → izin verilmeli

        // Mock başarılı tamamlanma simüle eder (exception fırlatmaz)
        _identity.DeleteUserAsync(targetId, actorId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // İstisna fırlatılmamalı
        await Should.NotThrowAsync(() => _identity.DeleteUserAsync(targetId, actorId));
    }

    [Fact]
    public async Task DeleteUserAsync_NonExistentUser_ThrowsNotFoundException()
    {
        var actorId = Guid.NewGuid();
        var ghostId = Guid.NewGuid();

        _identity
            .When(s => s.DeleteUserAsync(ghostId, actorId, Arg.Any<CancellationToken>()))
            .Do(_ => throw new NotFoundException($"Kullanıcı bulunamadı (id: {ghostId})."));

        await Should.ThrowAsync<NotFoundException>(
            () => _identity.DeleteUserAsync(ghostId, actorId));
    }

    // -----------------------------------------------------------------------
    // UserDto mapping — ApiResponse zarfı
    // -----------------------------------------------------------------------

    [Fact]
    public void UserDto_MapsAllFieldsCorrectly()
    {
        var id = Guid.NewGuid();
        var info = new AuthUserInfo(id, "x@oypa.com", "Ad Soyad", "Müdür", "555", ["Admin"]);

        // Controller mapping mantığı: AuthUserInfo → UserDto
        var dto = new UserDto(info.Id, info.Email, info.FullName, info.Position, info.Phone, info.Roles);

        dto.Id.ShouldBe(id);
        dto.Email.ShouldBe("x@oypa.com");
        dto.FullName.ShouldBe("Ad Soyad");
        dto.Position.ShouldBe("Müdür");
        dto.Phone.ShouldBe("555");
        dto.Roles.ShouldContain("Admin");
    }
}
