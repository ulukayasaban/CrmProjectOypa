using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Infrastructure.Persistence;
using Oypa.Crm.Infrastructure.Persistence.Repositories;
using Shouldly;

namespace Oypa.Crm.IntegrationTests;

/// <summary>
/// RefreshTokenRepository.DeleteExpiredAsync davranışını InMemory DB ile doğrular.
/// Süresi dolmuş token'ları siler; aktif (süresi dolmamış) token'lara dokunmaz.
/// </summary>
public sealed class RefreshTokenCleanupTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RefreshTokenCleanupTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static AppDbContext CreateFreshDbContext()
    {
        // Herbir test için bağımsız bir InMemory veritabanı: test yalıtımı sağlar.
        var uniqueDbName = $"cleanup-tests-{Guid.NewGuid():N}";

        var efServiceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(uniqueDbName)
            .UseInternalServiceProvider(efServiceProvider)
            .Options;

        // DomainEventDispatcher'ı stub geçiyoruz; cleanup testi event yayımlamaz.
        var dispatcher = NSubstitute.Substitute.For<Oypa.Crm.Application.Common.Events.IDomainEventDispatcher>();
        return new AppDbContext(options, dispatcher);
    }

    [Fact]
    public async Task DeleteExpiredAsync_ExpiredTokens_AreDeleted()
    {
        // Arrange
        await using var db = CreateFreshDbContext();
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var cutoff = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Süresi cutoff'tan önce dolan token (silinmeli)
        var expired1 = new RefreshToken(userId, "hash-expired-1", cutoff.AddDays(-10), null);
        var expired2 = new RefreshToken(userId, "hash-expired-2", cutoff.AddDays(-1), null);

        // Süresi cutoff'tan sonra dolan token (korunmalı)
        var active = new RefreshToken(userId, "hash-active", cutoff.AddDays(7), null);

        db.RefreshTokens.AddRange(expired1, expired2, active);
        await db.SaveChangesAsync();

        var repository = new RefreshTokenRepository(db);

        // Act
        var deletedCount = await repository.DeleteExpiredAsync(cutoff);

        // Assert
        deletedCount.ShouldBe(2, "Cutoff'tan önce süresi dolan 2 token silinmeli");

        var remaining = await db.RefreshTokens.ToListAsync();
        remaining.Count.ShouldBe(1, "Yalnızca aktif token kalmalı");
        remaining[0].TokenHash.ShouldBe("hash-active");
    }

    [Fact]
    public async Task DeleteExpiredAsync_NoExpiredTokens_ReturnsZero()
    {
        // Arrange — tüm token'ların süresi dolmamış
        await using var db = CreateFreshDbContext();
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var cutoff = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var active1 = new RefreshToken(userId, "hash-a1", cutoff.AddDays(5), null);
        var active2 = new RefreshToken(userId, "hash-a2", cutoff.AddDays(30), null);

        db.RefreshTokens.AddRange(active1, active2);
        await db.SaveChangesAsync();

        var repository = new RefreshTokenRepository(db);

        // Act
        var deletedCount = await repository.DeleteExpiredAsync(cutoff);

        // Assert
        deletedCount.ShouldBe(0, "Süresi dolmamış token'lara dokunulmamalı");

        var remaining = await db.RefreshTokens.ToListAsync();
        remaining.Count.ShouldBe(2);
    }

    [Fact]
    public async Task DeleteExpiredAsync_EmptyTable_ReturnsZero()
    {
        // Arrange — tablo boş
        await using var db = CreateFreshDbContext();
        await db.Database.EnsureCreatedAsync();

        var repository = new RefreshTokenRepository(db);
        var cutoff = DateTime.UtcNow;

        // Act
        var deletedCount = await repository.DeleteExpiredAsync(cutoff);

        // Assert
        deletedCount.ShouldBe(0);
    }

    [Fact]
    public async Task DeleteExpiredAsync_ActiveTokensPreserved_AfterCleanup()
    {
        // Arrange — karışık senaryo: bir kullanıcının hem süresi dolmuş hem aktif token'ları var
        await using var db = CreateFreshDbContext();
        await db.Database.EnsureCreatedAsync();

        var userId = Guid.NewGuid();
        var cutoff = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        var expired = new RefreshToken(userId, "old-hash", cutoff.AddDays(-31), null);
        var stillActive = new RefreshToken(userId, "new-hash", cutoff.AddDays(1), null);

        db.RefreshTokens.AddRange(expired, stillActive);
        await db.SaveChangesAsync();

        var repository = new RefreshTokenRepository(db);

        // Act
        await repository.DeleteExpiredAsync(cutoff);

        // Assert — aktif token hâlâ erişilebilir
        var found = await repository.GetByHashAsync("new-hash");
        found.ShouldNotBeNull("Aktif token DeleteExpiredAsync sonrası erişilebilir olmalı");
        found!.TokenHash.ShouldBe("new-hash");
    }
}
