using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Oypa.Crm.Contracts.Auth;
using Oypa.Crm.Contracts.Common;
using Oypa.Crm.Contracts.Goals;
using Oypa.Crm.Contracts.SalesReps;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Infrastructure.Identity;
using Oypa.Crm.Infrastructure.Persistence;
using Shouldly;

namespace Oypa.Crm.IntegrationTests;

/// <summary>
/// Goals API uçtan uca testleri: yetkilendirme, CRUD, haftalık snapshot,
/// SalesReps PATCH /{id}/employee yetki denetimi.
/// </summary>
public sealed class GoalsControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    // Seed'de tanımlı e-posta adresleri
    private const string UmurEmail = "umur.kutlu@oypa.com.tr";
    private const string AvniyeEmail = "avniye.oner@oypa.com.tr";
    private const string HalilEmail = "halil.kutukcu@oypa.com.tr";
    private const string MuhammedEmail = "muhammed.marangoz@oypa.com.tr";
    private const string OrgPassword = "Oypa!2026";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public GoalsControllerTests(CustomWebApplicationFactory factory)
    {
        // Rate limiter'ı devre dışı bırakarak türetilmiş bir host kullanıyoruz
        _factory = factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.PostConfigure<RateLimiterOptions>(options =>
                {
                    ClearRateLimiterPolicies(options);
                    foreach (var name in new[]
                    {
                        "auth-login",
                        "auth-refresh",
                        "urun-arama",
                        "admin-sensitive"
                    })
                    {
                        options.AddPolicy(name, _ => RateLimitPartition.GetNoLimiter("test"));
                    }
                });
            }));
        _client = _factory.CreateClient();
    }

    private static void ClearRateLimiterPolicies(RateLimiterOptions options)
    {
        foreach (var name in new[] { "PolicyMap", "UnactivatedPolicyMap" })
        {
            var member = typeof(RateLimiterOptions)
                .GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (member?.GetValue(options) is System.Collections.IDictionary dict)
                dict.Clear();
        }
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in new[] { "Admin", "Sales" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        await EnsureUserAsync(userManager,
            CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminPassword,
            "Test Yöneticisi",
            "Admin");

        await SeedOrgHierarchyAsync(db, userManager);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -----------------------------------------------------------------------
    // Yardımcı metotlar
    // -----------------------------------------------------------------------

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email, string password, string fullName, string role)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, role);
    }

    private static async Task SeedOrgHierarchyAsync(
        AppDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        if (await db.Employees.AnyAsync())
            return;

        var umur = new Employee("Pazarlama ve Satış Direktörü", "Umur KUTLU", UmurEmail);
        await LinkOrgAccountAsync(umur, UmurEmail, "Umur KUTLU", OrgPassword, "Admin", userManager);
        db.Employees.Add(umur);
        await db.SaveChangesAsync();

        var avniye = new Employee("Satış Müdürü", "Avniye Belgin ÖNER", AvniyeEmail, umur.Id);
        await LinkOrgAccountAsync(avniye, AvniyeEmail, "Avniye Belgin ÖNER", OrgPassword, "Admin", userManager);

        var halil = new Employee("Ticari Pazarlama Müdürü", "Halil Serdar KÜTÜKCÜ", HalilEmail, umur.Id);
        await LinkOrgAccountAsync(halil, HalilEmail, "Halil Serdar KÜTÜKCÜ", OrgPassword, "Admin", userManager);

        db.Employees.AddRange(avniye, halil);
        await db.SaveChangesAsync();

        var muhammed = new Employee("Satış Uzmanı", "Muhammed Safa MARANGOZ", MuhammedEmail, avniye.Id);
        await LinkOrgAccountAsync(muhammed, MuhammedEmail, "Muhammed Safa MARANGOZ", OrgPassword, "Sales", userManager);

        var turizmYon = new Employee("Turizm Satış ve İş Geliştirme Yöneticisi", managerId: avniye.Id);
        var satiOpsYon = new Employee("Satış Operasyonları ve Raporlama Yöneticisi", managerId: avniye.Id);
        var tesisYon = new Employee("Tesis Satış ve İş Geliştirme Yöneticisi", managerId: avniye.Id);
        var perakendeYon = new Employee("Perakende Satış Yöneticisi", managerId: avniye.Id);

        var pazarArastirma = new Employee("Pazar Araştırma ve Analiz Yöneticisi", managerId: halil.Id);
        var pazarGelistirme = new Employee("Pazar Geliştirme ve Fiyatlandırma Yöneticisi", managerId: halil.Id);

        var stajyer = new Employee("Stajyer", managerId: umur.Id);
        var saban = new Employee("Destek Personeli", "Şaban Ulukaya", "saban.ulukaya@oypa.com.tr", umur.Id);

        db.Employees.AddRange(
            muhammed,
            turizmYon, satiOpsYon, tesisYon, perakendeYon,
            pazarArastirma, pazarGelistirme,
            stajyer, saban);

        await db.SaveChangesAsync();
    }

    private static async Task LinkOrgAccountAsync(
        Employee employee,
        string email, string fullName, string password, string role,
        UserManager<ApplicationUser> userManager)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            employee.LinkAccount(existing.Id);
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            Position = employee.Title,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return;

        await userManager.AddToRoleAsync(user, role);
        employee.LinkAccount(user.Id);
    }

    private async Task<string> LoginAndGetTokenAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, password), JsonOptions);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"{email} ile giriş başarısız olmamalı");
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);
        return payload!.Data!.AccessToken;
    }

    private void SetBearer(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    /// <summary>Test için belirli e-postaya sahip Employee'nin Id'sini döndürür.</summary>
    private async Task<Guid> GetEmployeeIdAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emp = await db.Employees.SingleAsync(e => e.Email == email);
        return emp.Id;
    }

    // -----------------------------------------------------------------------
    // 401 — Anonim erişim
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetGoals_Anonymous_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/goals");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateGoal_Anonymous_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var umurId = await GetEmployeeIdAsync(UmurEmail);

        var response = await _client.PostAsJsonAsync("/api/goals",
            new CreateGoalRequest(umurId, "All", 5, null), JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // -----------------------------------------------------------------------
    // 403 — Kapsam dışı assignee
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateGoal_AsAvniye_AssigneeInHalilSubtree_Returns403()
    {
        // Avniye, Halil'in ağacındaki bir personele hedef atayamaz
        var avniyeToken = await LoginAndGetTokenAsync(AvniyeEmail, OrgPassword);
        SetBearer(avniyeToken);

        var halilId = await GetEmployeeIdAsync(HalilEmail);

        var response = await _client.PostAsJsonAsync("/api/goals",
            new CreateGoalRequest(halilId, "All", 5, "Halil için hedef"), JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateGoal_AsMuhammed_AsSalesUser_Returns403BecauseOutsideScope()
    {
        // Muhammed bir Sales kullanıcısı; Avniye'nin Id'sine hedef atamak kapsam dışı
        var muhammedToken = await LoginAndGetTokenAsync(MuhammedEmail, OrgPassword);
        SetBearer(muhammedToken);

        var avniyeId = await GetEmployeeIdAsync(AvniyeEmail);

        var response = await _client.PostAsJsonAsync("/api/goals",
            new CreateGoalRequest(avniyeId, "All", 5, null), JsonOptions);

        // Muhammed'in kapsamı sadece kendisi; Avniye kapsam dışı → 403
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // -----------------------------------------------------------------------
    // CRUD happy-path — Admin (Umur) tüm ağaca erişebilir
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateGoal_AsUmur_HappyPath_Returns201WithGoalDto()
    {
        // Arrange
        var umurToken = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(umurToken);

        var avniyeId = await GetEmployeeIdAsync(AvniyeEmail);

        // Act
        var response = await _client.PostAsJsonAsync("/api/goals",
            new CreateGoalRequest(avniyeId, "Lead", 8, "Avniye Lead Hedefi"), JsonOptions);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<GoalDto>>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Success.ShouldBeTrue();
        payload.Data!.AssigneeEmployeeId.ShouldBe(avniyeId);
        payload.Data.Segment.ShouldBe("Lead");
        payload.Data.WeeklyTarget.ShouldBe(8);
        payload.Data.Title.ShouldBe("Avniye Lead Hedefi");
        payload.Data.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateGoal_AsUmur_HappyPath_Returns200WithUpdatedData()
    {
        // Arrange — önce oluştur
        var umurToken = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(umurToken);

        var avniyeId = await GetEmployeeIdAsync(AvniyeEmail);

        var createResponse = await _client.PostAsJsonAsync("/api/goals",
            new CreateGoalRequest(avniyeId, "All", 5, "Başlangıç"), JsonOptions);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<GoalDto>>(JsonOptions))!.Data!;

        // Act
        var updateResponse = await _client.PutAsJsonAsync($"/api/goals/{created.Id}",
            new UpdateGoalRequest(avniyeId, "Customer", 10, "Güncel Başlık"), JsonOptions);

        // Assert
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = (await updateResponse.Content.ReadFromJsonAsync<ApiResponse<GoalDto>>(JsonOptions))!.Data!;
        updated.Segment.ShouldBe("Customer");
        updated.WeeklyTarget.ShouldBe(10);
        updated.Title.ShouldBe("Güncel Başlık");
    }

    [Fact]
    public async Task DeleteGoal_AsUmur_HappyPath_Returns200AndDeactivates()
    {
        // Arrange
        var umurToken = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(umurToken);

        var avniyeId = await GetEmployeeIdAsync(AvniyeEmail);

        var createResponse = await _client.PostAsJsonAsync("/api/goals",
            new CreateGoalRequest(avniyeId, "All", 3, "Silinecek"), JsonOptions);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<GoalDto>>(JsonOptions))!.Data!;

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/goals/{created.Id}");

        // Assert
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Silinen hedef artık GET listesinde aktif değil (IsActive=false)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var goal = await db.Goals.FindAsync(created.Id);
        goal.ShouldNotBeNull();
        goal!.IsActive.ShouldBeFalse("Hedef deaktif edilmiş olmalı");
    }

    [Fact]
    public async Task GetGoal_UnknownId_Returns404()
    {
        // Arrange
        var umurToken = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(umurToken);

        // Act
        var response = await _client.GetAsync($"/api/goals/{Guid.NewGuid()}/weeks");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // -----------------------------------------------------------------------
    // GET /{id}/weeks
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetGoalWeeks_AfterCreate_Returns200WithWeeksList()
    {
        // Arrange
        var umurToken = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(umurToken);

        var avniyeId = await GetEmployeeIdAsync(AvniyeEmail);

        var createResponse = await _client.PostAsJsonAsync("/api/goals",
            new CreateGoalRequest(avniyeId, "All", 5, "Haftalık Test"), JsonOptions);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<GoalDto>>(JsonOptions))!.Data!;

        // Act
        var weeksResponse = await _client.GetAsync($"/api/goals/{created.Id}/weeks");

        // Assert
        weeksResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await weeksResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<GoalWeekDto>>>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Success.ShouldBeTrue();
        payload.Data.ShouldNotBeNull();
        // En az içinde bulunulan haftanın snapshot'ı olmalı
        payload.Data!.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task GetGoalWeeks_AsAvniye_ForHalilGoal_Returns403()
    {
        // Arrange — Umur, Halil'e bir hedef oluşturur
        var umurToken = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(umurToken);

        var halilId = await GetEmployeeIdAsync(HalilEmail);

        var createResponse = await _client.PostAsJsonAsync("/api/goals",
            new CreateGoalRequest(halilId, "All", 5, "Halil Hedefi"), JsonOptions);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<GoalDto>>(JsonOptions))!.Data!;

        // Avniye token ile Halil'in hedefinin haftalarına erişmeye çalış
        var avniyeToken = await LoginAndGetTokenAsync(AvniyeEmail, OrgPassword);
        SetBearer(avniyeToken);

        // Act
        var weeksResponse = await _client.GetAsync($"/api/goals/{created.Id}/weeks");

        // Assert
        weeksResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // -----------------------------------------------------------------------
    // GET /api/goals — Avniye kendi alt-ağacını, Halil'inkini görmez
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetGoals_AsAvniye_SeesOwnScopedGoals_NotHalilsGoals()
    {
        // Arrange — Umur, Halil'e hedef oluşturur; Avniye göremez
        var umurToken = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(umurToken);

        var halilId = await GetEmployeeIdAsync(HalilEmail);
        var avniyeId = await GetEmployeeIdAsync(AvniyeEmail);

        var createHalilGoalResponse = await _client.PostAsJsonAsync("/api/goals",
            new CreateGoalRequest(halilId, "All", 5, "Halil Hedefi"), JsonOptions);
        createHalilGoalResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var halilGoal = (await createHalilGoalResponse.Content.ReadFromJsonAsync<ApiResponse<GoalDto>>(JsonOptions))!.Data!;

        var createAvniyeGoalResponse = await _client.PostAsJsonAsync("/api/goals",
            new CreateGoalRequest(avniyeId, "Lead", 4, "Avniye Hedefi"), JsonOptions);
        createAvniyeGoalResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var avniyeGoal = (await createAvniyeGoalResponse.Content.ReadFromJsonAsync<ApiResponse<GoalDto>>(JsonOptions))!.Data!;

        // Avniye token ile listele
        var avniyeToken = await LoginAndGetTokenAsync(AvniyeEmail, OrgPassword);
        SetBearer(avniyeToken);

        // Act
        var response = await _client.GetAsync("/api/goals");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<GoalDto>>>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Success.ShouldBeTrue();

        var goalIds = payload.Data!.Select(g => g.Id).ToHashSet();
        goalIds.ShouldNotContain(halilGoal.Id, "Avniye Halil'in hedefini göremez");
        goalIds.ShouldContain(avniyeGoal.Id, "Avniye kendi hedefini görmeli");
    }

    // -----------------------------------------------------------------------
    // SalesReps PATCH /{id}/employee — yetki testleri
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PatchSalesRepEmployee_AsAdmin_Returns200WithEmployeeId()
    {
        // Arrange
        var umurToken = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(umurToken);

        // Satış temsilcisi oluştur
        var createRepResponse = await _client.PostAsJsonAsync("/api/salesreps",
            new CreateSalesRepRequest("Bağlanacak Temsilci", $"link.{Guid.NewGuid():N}@oypa.com"), JsonOptions);
        createRepResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var rep = (await createRepResponse.Content.ReadFromJsonAsync<ApiResponse<SalesRepDto>>(JsonOptions))!.Data!;

        var avniyeId = await GetEmployeeIdAsync(AvniyeEmail);

        // Act — temsilciyi personele bağla
        var patchResponse = await _client.PatchAsJsonAsync(
            $"/api/salesreps/{rep.Id}/employee",
            new LinkEmployeeRequest(avniyeId),
            JsonOptions);

        // Assert
        patchResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await patchResponse.Content.ReadFromJsonAsync<ApiResponse<SalesRepDto>>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Success.ShouldBeTrue();
        payload.Data!.EmployeeId.ShouldBe(avniyeId);
    }

    [Fact]
    public async Task PatchSalesRepEmployee_AsNonAdmin_Returns403()
    {
        // Arrange — Umur ile temsilci oluştur
        var umurToken = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(umurToken);

        var createRepResponse = await _client.PostAsJsonAsync("/api/salesreps",
            new CreateSalesRepRequest("Yasak Temsilci", $"yasak.{Guid.NewGuid():N}@oypa.com"), JsonOptions);
        createRepResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var rep = (await createRepResponse.Content.ReadFromJsonAsync<ApiResponse<SalesRepDto>>(JsonOptions))!.Data!;

        // Muhammed (Sales) ile patch dene
        var muhammedToken = await LoginAndGetTokenAsync(MuhammedEmail, OrgPassword);
        SetBearer(muhammedToken);

        var avniyeId = await GetEmployeeIdAsync(AvniyeEmail);

        // Act
        var patchResponse = await _client.PatchAsJsonAsync(
            $"/api/salesreps/{rep.Id}/employee",
            new LinkEmployeeRequest(avniyeId),
            JsonOptions);

        // Assert
        patchResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PatchSalesRepEmployee_Anonymous_Returns401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var patchResponse = await _client.PatchAsJsonAsync(
            $"/api/salesreps/{Guid.NewGuid()}/employee",
            new LinkEmployeeRequest(null),
            JsonOptions);

        // Assert
        patchResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PatchSalesRepEmployee_UnknownRepId_Returns404()
    {
        // Arrange
        var umurToken = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(umurToken);

        // Act
        var patchResponse = await _client.PatchAsJsonAsync(
            $"/api/salesreps/{Guid.NewGuid()}/employee",
            new LinkEmployeeRequest(null),
            JsonOptions);

        // Assert
        patchResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
