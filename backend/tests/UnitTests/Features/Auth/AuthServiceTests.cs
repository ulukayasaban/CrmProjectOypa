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

public sealed class AuthServiceTests
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
    private readonly IAuditLogWriter _auditLogWriter = Substitute.For<IAuditLogWriter>();
    private readonly IOptions<JwtOptions> _jwtOptions =
        Options.Create(new JwtOptions { RefreshTokenDays = 7, AccessTokenMinutes = 15 });
    private readonly IOptions<AppOptions> _appOptions =
        Options.Create(new AppOptions { FrontendBaseUrl = "http://localhost:5173" });

    private readonly AuthUserInfo _user = new(
        Guid.NewGuid(), "admin@oypa.com.tr", "Admin", "Pos", "555", new[] { "Admin" });

    private AuthService CreateSut() =>
        new(_identity, _jwt, _refreshTokens, _unitOfWork, _currentUser, _clock, _jwtOptions, _emailSender, _appOptions, _employees, _salesReps, _auditLogWriter);

    public AuthServiceTests()
    {
        _clock.UtcNow.Returns(new DateTime(2026, 6, 8, 0, 0, 0, DateTimeKind.Utc));
        _jwt.CreateAccessToken(Arg.Any<AuthUserInfo>())
            .Returns(new TokenResult("access-token", new DateTime(2026, 6, 8, 0, 15, 0, DateTimeKind.Utc)));
        _jwt.GenerateRefreshToken().Returns("raw-refresh");
        _jwt.HashToken(Arg.Any<string>()).Returns(ci => "hash:" + ci.Arg<string>());

        // Varsayılan: boş liste döndür; GetCurrentUserAsync testleri kendi stub'larını tanımlar.
        _employees.ListAsync(Arg.Any<Expression<Func<Employee, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Employee>());
        _salesReps.ListAsync(Arg.Any<Expression<Func<SalesRep, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SalesRep>());
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResponseAndPersistsRefreshToken()
    {
        _identity.ValidateCredentialsAsync(_user.Email, "pwd", Arg.Any<CancellationToken>())
            .Returns(_user);
        var sut = CreateSut();

        var result = await sut.LoginAsync(new LoginRequest(_user.Email, "pwd"), "127.0.0.1");

        result.AccessToken.ShouldBe("access-token");
        result.RefreshToken.ShouldBe("raw-refresh");
        result.User.Email.ShouldBe(_user.Email);
        await _refreshTokens.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_InvalidCredentials_ThrowsUnauthorized()
    {
        _identity.ValidateCredentialsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((AuthUserInfo?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<UnauthorizedAppException>(
            () => sut.LoginAsync(new LoginRequest("x@y.z", "bad"), null));

        await _refreshTokens.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_RevokedToken_RevokesActiveTokensAndThrows()
    {
        var revoked = new RefreshToken(_user.Id, "hash:raw-refresh", _clock.UtcNow.AddDays(7), null);
        revoked.Revoke();
        _refreshTokens.GetByHashAsync("hash:raw-refresh", Arg.Any<CancellationToken>()).Returns(revoked);

        var active1 = new RefreshToken(_user.Id, "h1", _clock.UtcNow.AddDays(7), null);
        var active2 = new RefreshToken(_user.Id, "h2", _clock.UtcNow.AddDays(7), null);
        _refreshTokens.GetActiveByUserAsync(_user.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { active1, active2 });

        var sut = CreateSut();

        await Should.ThrowAsync<UnauthorizedAppException>(
            () => sut.RefreshAsync("raw-refresh", null));

        active1.IsRevoked.ShouldBeTrue();
        active2.IsRevoked.ShouldBeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_ValidToken_RotatesAndIssuesNewToken()
    {
        var existing = new RefreshToken(_user.Id, "hash:raw-refresh", _clock.UtcNow.AddDays(7), null);
        _refreshTokens.GetByHashAsync("hash:raw-refresh", Arg.Any<CancellationToken>()).Returns(existing);
        _identity.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _jwt.GenerateRefreshToken().Returns("new-raw");

        var sut = CreateSut();

        var result = await sut.RefreshAsync("raw-refresh", "10.0.0.1");

        existing.IsRevoked.ShouldBeTrue();
        existing.ReplacedByTokenHash.ShouldBe("hash:new-raw");
        result.RefreshToken.ShouldBe("new-raw");
        result.AccessToken.ShouldBe("access-token");
        await _refreshTokens.Received(1).AddAsync(
            Arg.Is<RefreshToken>(t => t.TokenHash == "hash:new-raw"), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ========================================================================
    // GetCurrentUserAsync — AssignedSalesRepId çözümlemesi
    // ========================================================================

    [Fact]
    public async Task GetCurrentUserAsync_WhenUserHasLinkedSalesRep_ReturnsAssignedSalesRepId()
    {
        // Arrange — ApplicationUser → Employee → SalesRep zinciri kuruldu
        var userId = _user.Id;
        var employeeId = Guid.NewGuid();
        var salesRepId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _identity.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(_user);

        var employee = new Employee("Unvan", "Admin", "admin@oypa.com.tr");
        employee.Id = employeeId;
        employee.LinkAccount(userId);

        var salesRep = new SalesRep("Admin Rep", "admin-rep@oypa.com.tr");
        salesRep.Id = salesRepId;
        salesRep.LinkEmployee(employeeId);

        _employees.ListAsync(Arg.Any<Expression<Func<Employee, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { employee });
        _salesReps.ListAsync(Arg.Any<Expression<Func<SalesRep, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { salesRep });

        var sut = CreateSut();

        // Act
        var result = await sut.GetCurrentUserAsync();

        // Assert
        result.AssignedSalesRepId.ShouldBe(salesRepId);
        result.Email.ShouldBe(_user.Email);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WhenUserHasNoLinkedEmployee_AssignedSalesRepIdIsNull()
    {
        // Arrange — Employee kaydı yok
        var userId = _user.Id;

        _currentUser.UserId.Returns(userId);
        _identity.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(_user);

        // Varsayılan stub'lar zaten boş liste döndürüyor (kurucu içinde tanımlı)

        var sut = CreateSut();

        // Act
        var result = await sut.GetCurrentUserAsync();

        // Assert
        result.AssignedSalesRepId.ShouldBeNull();
    }
}
