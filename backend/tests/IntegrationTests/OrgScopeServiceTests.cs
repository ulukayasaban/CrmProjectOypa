using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Oypa.Crm.Application.Common.Events;
using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Domain.Common;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Infrastructure.Features.Org;
using Oypa.Crm.Infrastructure.Persistence;
using Shouldly;

namespace Oypa.Crm.IntegrationTests;

/// <summary>
/// OrgScopeService birim/entegrasyon testleri.
/// InMemory DbContext üzerinden BFS algoritması ve kapsam çözümleme mantığı doğrulanır.
/// Her test metodu izole bir InMemory veritabanı kullanır.
/// </summary>
public sealed class OrgScopeServiceTests : IAsyncLifetime
{
    private AppDbContext _db = null!;

    // Testler arası izolasyon için benzersiz DB adı
    private static string NewDbName() => $"OrgScopeTests-{Guid.NewGuid()}";

    public async Task InitializeAsync()
    {
        _db = CreateDb();
        await _db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
    }

    private static AppDbContext CreateDb()
    {
        // No-op dispatcher: domain olayları InMemory testlerinde göz ardı edilir.
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        dispatcher.DispatchAsync(Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(NewDbName())
            .Options;

        return new AppDbContext(options, dispatcher);
    }

    // -----------------------------------------------------------------------
    // Yardımcı fabrika metotları
    // -----------------------------------------------------------------------

    private OrgScopeService CreateSut(ICurrentUser currentUser) =>
        new(_db, currentUser);

    private static ICurrentUser AdminUser(Guid userId) =>
        MockUser(userId, isAuthenticated: true, roles: ["Admin"]);

    private static ICurrentUser AuthenticatedUser(Guid userId) =>
        MockUser(userId, isAuthenticated: true, roles: ["Sales"]);

    private static ICurrentUser UnauthenticatedUser() =>
        MockUser(Guid.Empty, isAuthenticated: false, roles: []);

    private static ICurrentUser MockUser(Guid userId, bool isAuthenticated, string[] roles)
    {
        var user = Substitute.For<ICurrentUser>();
        user.UserId.Returns(isAuthenticated ? userId : (Guid?)null);
        user.IsAuthenticated.Returns(isAuthenticated);
        user.Roles.Returns(roles);
        return user;
    }

    // -----------------------------------------------------------------------
    // ResolveAsync — kimlik doğrulama testleri
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ResolveAsync_Unauthenticated_ThrowsForbidden()
    {
        var sut = CreateSut(UnauthenticatedUser());

        await Should.ThrowAsync<ForbiddenAppException>(() => sut.ResolveAsync());
    }

    [Fact]
    public async Task ResolveAsync_AuthenticatedUserNotLinkedToEmployee_NonAdmin_ThrowsForbidden()
    {
        // Org'a bağlı Employee kaydı yok; Admin rolü de yok → 403
        var userId = Guid.NewGuid();
        var sut = CreateSut(AuthenticatedUser(userId));

        await Should.ThrowAsync<ForbiddenAppException>(() => sut.ResolveAsync());
    }

    [Fact]
    public async Task ResolveAsync_AuthenticatedAdminNotLinkedToEmployee_ReturnsAllEmployeesScope()
    {
        // Admin rolü var ama Employee'ye bağlı değil → tüm personel kapsamı
        var userId = Guid.NewGuid();
        var sut = CreateSut(AdminUser(userId));

        var scope = await sut.ResolveAsync();

        scope.AllEmployees.ShouldBeTrue();
        scope.Ids.ShouldBeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_RootEmployee_ReturnsAllEmployeesScope()
    {
        // Kök düğüm (ManagerId == null) → tüm personel
        var userId = Guid.NewGuid();
        var root = new Employee("Direktör", "Umur KUTLU", "umur@oypa.com");
        root.LinkAccount(userId);
        _db.Employees.Add(root);
        await _db.SaveChangesAsync();

        var sut = CreateSut(AuthenticatedUser(userId));

        var scope = await sut.ResolveAsync();

        scope.AllEmployees.ShouldBeTrue();
    }

    [Fact]
    public async Task ResolveAsync_NonRootEmployee_ReturnsSubtreeScope()
    {
        // Orta düzey yönetici: kendi alt-ağacını görür
        var rootUserId = Guid.NewGuid();
        var managerUserId = Guid.NewGuid();

        var root = new Employee("Direktör", "Umur KUTLU", "umur@oypa.com");
        root.LinkAccount(rootUserId);
        _db.Employees.Add(root);
        await _db.SaveChangesAsync();

        var manager = new Employee("Müdür", "Avniye ÖNER", "avniye@oypa.com", root.Id);
        manager.LinkAccount(managerUserId);
        _db.Employees.Add(manager);
        await _db.SaveChangesAsync();

        var sut = CreateSut(AuthenticatedUser(managerUserId));

        var scope = await sut.ResolveAsync();

        scope.AllEmployees.ShouldBeFalse();
        // Kendi Id'si dahil subtree'sini görür
        scope.Ids.ShouldContain(manager.Id);
    }

    // -----------------------------------------------------------------------
    // GetSubtreeIdsAsync — BFS doğrulama
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetSubtreeIdsAsync_RootWithNoChildren_ReturnsSingletonSet()
    {
        var root = new Employee("Direktör");
        _db.Employees.Add(root);
        await _db.SaveChangesAsync();

        var sut = CreateSut(UnauthenticatedUser());

        var ids = await sut.GetSubtreeIdsAsync(root.Id);

        ids.Count.ShouldBe(1);
        ids.ShouldContain(root.Id);
    }

    [Fact]
    public async Task GetSubtreeIdsAsync_ThreeLevelHierarchy_ReturnsAllDescendants()
    {
        // Hiyerarşi: root → child1, child2 → grandchild (child1 altında)
        var root = new Employee("Kök");
        _db.Employees.Add(root);
        await _db.SaveChangesAsync();

        var child1 = new Employee("Çocuk1", managerId: root.Id);
        var child2 = new Employee("Çocuk2", managerId: root.Id);
        _db.Employees.AddRange(child1, child2);
        await _db.SaveChangesAsync();

        var grandchild = new Employee("TorunNode", managerId: child1.Id);
        _db.Employees.Add(grandchild);
        await _db.SaveChangesAsync();

        var sut = CreateSut(UnauthenticatedUser());

        var ids = await sut.GetSubtreeIdsAsync(root.Id);

        ids.Count.ShouldBe(4);
        ids.ShouldContain(root.Id);
        ids.ShouldContain(child1.Id);
        ids.ShouldContain(child2.Id);
        ids.ShouldContain(grandchild.Id);
    }

    [Fact]
    public async Task GetSubtreeIdsAsync_ChildNodeAsRoot_ReturnsOnlySubtree()
    {
        // Sadece child2 alt-ağacını sor → kökü ve kardeş alt-ağacı görünmemeli
        var root = new Employee("Kök");
        _db.Employees.Add(root);
        await _db.SaveChangesAsync();

        var child1 = new Employee("Çocuk1", managerId: root.Id);
        var child2 = new Employee("Çocuk2", managerId: root.Id);
        _db.Employees.AddRange(child1, child2);
        await _db.SaveChangesAsync();

        var sut = CreateSut(UnauthenticatedUser());

        var ids = await sut.GetSubtreeIdsAsync(child2.Id);

        ids.Count.ShouldBe(1);
        ids.ShouldContain(child2.Id);
        ids.ShouldNotContain(root.Id);
        ids.ShouldNotContain(child1.Id);
    }

    // -----------------------------------------------------------------------
    // GetSubtreeUserIdsAsync — hesaplı kullanıcıları filtreler
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetSubtreeUserIdsAsync_OnlyLinkedUsersReturned()
    {
        var linkedUserId = Guid.NewGuid();

        var root = new Employee("Kök");
        _db.Employees.Add(root);
        await _db.SaveChangesAsync();

        // child1 hesaplı, child2 hesapsız
        var child1 = new Employee("Hesaplı", managerId: root.Id);
        child1.LinkAccount(linkedUserId);
        var child2 = new Employee("Hesapsız", managerId: root.Id);
        _db.Employees.AddRange(child1, child2);
        await _db.SaveChangesAsync();

        var sut = CreateSut(UnauthenticatedUser());

        var userIds = await sut.GetSubtreeUserIdsAsync(root.Id);

        userIds.Count.ShouldBe(1);
        userIds.ShouldContain(linkedUserId);
    }

    [Fact]
    public async Task GetSubtreeUserIdsAsync_NoLinkedUsers_ReturnsEmptyList()
    {
        var root = new Employee("Hesapsız Kök");
        _db.Employees.Add(root);
        await _db.SaveChangesAsync();

        var child = new Employee("Hesapsız Çocuk", managerId: root.Id);
        _db.Employees.Add(child);
        await _db.SaveChangesAsync();

        var sut = CreateSut(UnauthenticatedUser());

        var userIds = await sut.GetSubtreeUserIdsAsync(root.Id);

        userIds.ShouldBeEmpty();
    }

    // -----------------------------------------------------------------------
    // GetAncestorUserIdsAsync — yönetici zinciri
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAncestorUserIdsAsync_ThreeLevels_ReturnsManagerChainUserIds()
    {
        var rootUserId = Guid.NewGuid();
        var midUserId = Guid.NewGuid();

        var root = new Employee("Direktör");
        root.LinkAccount(rootUserId);
        _db.Employees.Add(root);
        await _db.SaveChangesAsync();

        var mid = new Employee("Müdür", managerId: root.Id);
        mid.LinkAccount(midUserId);
        _db.Employees.Add(mid);
        await _db.SaveChangesAsync();

        var leaf = new Employee("Uzman", managerId: mid.Id);
        _db.Employees.Add(leaf);
        await _db.SaveChangesAsync();

        var sut = CreateSut(UnauthenticatedUser());

        // Yaprak düğümün yönetici zinciri: mid → root
        var ancestorUserIds = await sut.GetAncestorUserIdsAsync(leaf.Id);

        ancestorUserIds.Count.ShouldBe(2);
        ancestorUserIds.ShouldContain(midUserId);
        ancestorUserIds.ShouldContain(rootUserId);
    }

    [Fact]
    public async Task GetAncestorUserIdsAsync_UnlinkedAncestors_NotIncludedInResult()
    {
        // Hesapsız ara yönetici sonuçta yer almamalı
        var rootUserId = Guid.NewGuid();

        var root = new Employee("Direktör");
        root.LinkAccount(rootUserId);
        _db.Employees.Add(root);
        await _db.SaveChangesAsync();

        var mid = new Employee("Hesapsız Müdür", managerId: root.Id);
        _db.Employees.Add(mid);
        await _db.SaveChangesAsync();

        var leaf = new Employee("Uzman", managerId: mid.Id);
        _db.Employees.Add(leaf);
        await _db.SaveChangesAsync();

        var sut = CreateSut(UnauthenticatedUser());

        var ancestorUserIds = await sut.GetAncestorUserIdsAsync(leaf.Id);

        // Hesapsız ara yönetici atlanmalı; sadece root (hesaplı) dönmeli
        ancestorUserIds.Count.ShouldBe(1);
        ancestorUserIds.ShouldContain(rootUserId);
    }

    [Fact]
    public async Task GetAncestorUserIdsAsync_RootNode_ReturnsEmptyList()
    {
        // Kök düğümün yöneticisi yok
        var root = new Employee("Kök");
        _db.Employees.Add(root);
        await _db.SaveChangesAsync();

        var sut = CreateSut(UnauthenticatedUser());

        var ancestorUserIds = await sut.GetAncestorUserIdsAsync(root.Id);

        ancestorUserIds.ShouldBeEmpty();
    }

    // -----------------------------------------------------------------------
    // GetByUserIdAsync — kullanıcıya bağlı Employee
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetByUserIdAsync_LinkedEmployee_ReturnsEmployee()
    {
        var userId = Guid.NewGuid();
        var employee = new Employee("Test", "Ad Soyad", "ad@oypa.com");
        employee.LinkAccount(userId);
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        var sut = CreateSut(UnauthenticatedUser());

        var result = await sut.GetByUserIdAsync(userId);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(employee.Id);
    }

    [Fact]
    public async Task GetByUserIdAsync_UnknownUserId_ReturnsNull()
    {
        var sut = CreateSut(UnauthenticatedUser());

        var result = await sut.GetByUserIdAsync(Guid.NewGuid());

        result.ShouldBeNull();
    }
}
