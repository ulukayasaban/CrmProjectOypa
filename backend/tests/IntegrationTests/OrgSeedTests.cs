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
using Oypa.Crm.Contracts.Employees;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Infrastructure.Identity;
using Oypa.Crm.Infrastructure.Persistence;
using Shouldly;

namespace Oypa.Crm.IntegrationTests;

/// <summary>
/// Organizasyon hiyerarşisi seed'ini, hesap/rol atamasını, idempotency davranışını
/// ve GET /api/employees endpoint'ini doğrular.
/// </summary>
public sealed class OrgSeedTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    // Sabit e-posta adresleri — DbSeeder ile birebir eşleşmeli.
    private const string UmurEmail = "umur.kutlu@oypa.com.tr";
    private const string AvniyeEmail = "avniye.oner@oypa.com.tr";
    private const string HalilEmail = "halil.kutukcu@oypa.com.tr";
    private const string MuhammedEmail = "muhammed.marangoz@oypa.com.tr";
    private const string YigitEmail = "yigit.ersoy@oypa.com.tr";
    private const string IsmailEmail = "ismail.tazecan@oypa.com.tr";
    private const string SabanEmail = "saban.ulukaya@oypa.com.tr";
    private const string OrgPassword = "Oypa!2026";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public OrgSeedTests(CustomWebApplicationFactory factory)
    {
        // Rate limiter'ı devre dışı bırakarak türetilmiş bir host kullanıyoruz;
        // orijinal factory'ye dokunmuyoruz.
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

    /// <summary>
    /// Roller, admin kullanıcı ve organizasyon hiyerarşisini seed eder.
    /// DbSeeder.SeedOrgAsync private olduğundan mantığı burada çoğaltıyoruz.
    /// </summary>
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

        // Test host için admin kullanıcı (endpoint testleri için gerekli).
        await EnsureUserAsync(userManager,
            CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminPassword,
            "Test Yöneticisi",
            "Admin");

        // Organizasyon hiyerarşisini seed et (idempotent).
        await SeedOrgHierarchyAsync(db, userManager);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// DbSeeder.SeedOrgAsync ile birebir aynı hiyerarşiyi oluşturur.
    /// Employees tablosu boşsa çalışır (idempotent davranış).
    /// </summary>
    private static async Task SeedOrgHierarchyAsync(
        AppDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        if (await db.Employees.AnyAsync())
            return;

        // --- Kök: Umur KUTLU (Direktör / Admin) ---
        var umur = new Employee("Pazarlama ve Satış Direktörü", "Umur KUTLU", UmurEmail);
        await LinkOrgAccountAsync(umur, UmurEmail, "Umur KUTLU", OrgPassword, "Admin", userManager);
        db.Employees.Add(umur);
        await db.SaveChangesAsync();

        // --- Müdürler (Umur'a bağlı / Admin) ---
        var avniye = new Employee("Satış Müdürü", "Avniye Belgin ÖNER", AvniyeEmail, umur.Id);
        await LinkOrgAccountAsync(avniye, AvniyeEmail, "Avniye Belgin ÖNER", OrgPassword, "Admin", userManager);

        var halil = new Employee("Ticari Pazarlama Müdürü", "Halil Serdar KÜTÜKCÜ", HalilEmail, umur.Id);
        await LinkOrgAccountAsync(halil, HalilEmail, "Halil Serdar KÜTÜKCÜ", OrgPassword, "Admin", userManager);

        db.Employees.AddRange(avniye, halil);
        await db.SaveChangesAsync();

        // --- Avniye'ye bağlı personel ---
        var muhammed = new Employee("Satış Uzmanı", "Muhammed Safa MARANGOZ", MuhammedEmail, avniye.Id);
        await LinkOrgAccountAsync(muhammed, MuhammedEmail, "Muhammed Safa MARANGOZ", OrgPassword, "Sales", userManager);

        var yigit = new Employee("Satış Uzmanı", "Yiğit ERSOY", YigitEmail, avniye.Id);
        await LinkOrgAccountAsync(yigit, YigitEmail, "Yiğit ERSOY", OrgPassword, "Sales", userManager);

        var ismail = new Employee("Depo Şefi", "İsmail TAZECAN", IsmailEmail, avniye.Id);
        await LinkOrgAccountAsync(ismail, IsmailEmail, "İsmail TAZECAN", OrgPassword, "Sales", userManager);

        // Hesapsız düğümler — Avniye'ye bağlı
        var turizmYon = new Employee("Turizm Satış ve İş Geliştirme Yöneticisi", managerId: avniye.Id);
        var satiOpsYon = new Employee("Satış Operasyonları ve Raporlama Yöneticisi", managerId: avniye.Id);
        var tesisYon = new Employee("Tesis Satış ve İş Geliştirme Yöneticisi", managerId: avniye.Id);
        var perakendeYon = new Employee("Perakende Satış Yöneticisi", managerId: avniye.Id);

        // --- Halil'e bağlı hesapsız düğümler ---
        var pazarArastirma = new Employee("Pazar Araştırma ve Analiz Yöneticisi", managerId: halil.Id);
        var pazarGelistirme = new Employee("Pazar Geliştirme ve Fiyatlandırma Yöneticisi", managerId: halil.Id);

        // --- Umur'a bağlı kalan düğümler ---
        var stajyer = new Employee("Stajyer", managerId: umur.Id);

        var saban = new Employee("Destek Personeli", "Şaban Ulukaya", SabanEmail, umur.Id);
        await LinkOrgAccountAsync(saban, SabanEmail, "Şaban Ulukaya", OrgPassword, "Sales", userManager);

        db.Employees.AddRange(
            muhammed, yigit, ismail,
            turizmYon, satiOpsYon, tesisYon, perakendeYon,
            pazarArastirma, pazarGelistirme,
            stajyer, saban);

        await db.SaveChangesAsync();
    }

    private static async Task LinkOrgAccountAsync(
        Employee employee,
        string email,
        string fullName,
        string password,
        string role,
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

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string fullName,
        string role)
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

    private async Task<string> LoginAndGetTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword),
            JsonOptions);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);
        return payload!.Data!.AccessToken;
    }

    // ---- Seed / Hiyerarşi testleri ----

    [Fact]
    public async Task SeedOrg_CreatesExactly14Employees()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var count = await db.Employees.CountAsync();
        count.ShouldBe(14);
    }

    [Fact]
    public async Task SeedOrg_Umur_IsRootWithNullManagerId()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var umur = await db.Employees.SingleAsync(e => e.Email == UmurEmail);
        umur.ManagerId.ShouldBeNull();
        umur.FullName.ShouldBe("Umur KUTLU");
    }

    [Fact]
    public async Task SeedOrg_AvniyeAndHalil_HaveUmurAsManager()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var umur = await db.Employees.SingleAsync(e => e.Email == UmurEmail);
        var avniye = await db.Employees.SingleAsync(e => e.Email == AvniyeEmail);
        var halil = await db.Employees.SingleAsync(e => e.Email == HalilEmail);

        avniye.ManagerId.ShouldBe(umur.Id);
        halil.ManagerId.ShouldBe(umur.Id);
    }

    [Fact]
    public async Task SeedOrg_MuhammedYigitIsmail_HaveAvniyeAsManager()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var avniye = await db.Employees.SingleAsync(e => e.Email == AvniyeEmail);

        var muhammed = await db.Employees.SingleAsync(e => e.Email == MuhammedEmail);
        var yigit = await db.Employees.SingleAsync(e => e.Email == YigitEmail);
        var ismail = await db.Employees.SingleAsync(e => e.Email == IsmailEmail);

        muhammed.ManagerId.ShouldBe(avniye.Id);
        yigit.ManagerId.ShouldBe(avniye.Id);
        ismail.ManagerId.ShouldBe(avniye.Id);
    }

    [Fact]
    public async Task SeedOrg_FourUnnamedNodes_HaveAvniyeAsManager()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var avniye = await db.Employees.SingleAsync(e => e.Email == AvniyeEmail);

        var unnamedUnderAvniye = await db.Employees
            .Where(e => e.ManagerId == avniye.Id && e.ApplicationUserId == null)
            .CountAsync();

        unnamedUnderAvniye.ShouldBe(4);
    }

    [Fact]
    public async Task SeedOrg_TwoUnnamedNodes_HaveHalilAsManager()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var halil = await db.Employees.SingleAsync(e => e.Email == HalilEmail);

        var unnamedUnderHalil = await db.Employees
            .Where(e => e.ManagerId == halil.Id && e.ApplicationUserId == null)
            .CountAsync();

        unnamedUnderHalil.ShouldBe(2);
    }

    [Fact]
    public async Task SeedOrg_StajyerAndSaban_HaveUmurAsManager()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var umur = await db.Employees.SingleAsync(e => e.Email == UmurEmail);
        var saban = await db.Employees.SingleAsync(e => e.Email == SabanEmail);
        var stajyer = await db.Employees.SingleAsync(e => e.Title == "Stajyer");

        saban.ManagerId.ShouldBe(umur.Id);
        stajyer.ManagerId.ShouldBe(umur.Id);
    }

    [Fact]
    public async Task SeedOrg_VerifiesExpectedEmailAddresses()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var emails = await db.Employees
            .Where(e => e.Email != null)
            .Select(e => e.Email!)
            .ToListAsync();

        emails.ShouldContain(UmurEmail);
        emails.ShouldContain(AvniyeEmail);
        emails.ShouldContain(HalilEmail);
        emails.ShouldContain(MuhammedEmail);
        emails.ShouldContain(YigitEmail);
        emails.ShouldContain(IsmailEmail);
        emails.ShouldContain(SabanEmail);
    }

    // ---- Hesap / Rol testleri ----

    [Fact]
    public async Task SeedOrg_Named7Employees_HaveApplicationUserId()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var namedEmails = new[]
        {
            UmurEmail, AvniyeEmail, HalilEmail,
            MuhammedEmail, YigitEmail, IsmailEmail, SabanEmail
        };

        foreach (var email in namedEmails)
        {
            var employee = await db.Employees.SingleAsync(e => e.Email == email);
            employee.ApplicationUserId.ShouldNotBeNull(
                $"{email} için ApplicationUserId dolu olmalı");
        }
    }

    [Fact]
    public async Task SeedOrg_Unnamed7Nodes_HaveNullApplicationUserId()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var unnamedCount = await db.Employees
            .Where(e => e.ApplicationUserId == null)
            .CountAsync();

        unnamedCount.ShouldBe(7);
    }

    [Fact]
    public async Task SeedOrg_AdminTriple_HaveAdminRole()
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppDbContext>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        var adminEmails = new[] { UmurEmail, AvniyeEmail, HalilEmail };

        foreach (var email in adminEmails)
        {
            var employee = await db.Employees.SingleAsync(e => e.Email == email);
            employee.ApplicationUserId.ShouldNotBeNull();

            var user = await userManager.FindByIdAsync(employee.ApplicationUserId!.Value.ToString());
            user.ShouldNotBeNull($"{email} için ApplicationUser bulunmalı");

            var roles = await userManager.GetRolesAsync(user!);
            roles.ShouldContain("Admin", $"{email} Admin rolüne sahip olmalı");
        }
    }

    [Fact]
    public async Task SeedOrg_SalesQuartet_HaveSalesRole()
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppDbContext>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        var salesEmails = new[] { MuhammedEmail, YigitEmail, IsmailEmail, SabanEmail };

        foreach (var email in salesEmails)
        {
            var employee = await db.Employees.SingleAsync(e => e.Email == email);
            employee.ApplicationUserId.ShouldNotBeNull();

            var user = await userManager.FindByIdAsync(employee.ApplicationUserId!.Value.ToString());
            user.ShouldNotBeNull($"{email} için ApplicationUser bulunmalı");

            var roles = await userManager.GetRolesAsync(user!);
            roles.ShouldContain("Sales", $"{email} Sales rolüne sahip olmalı");
        }
    }

    [Fact]
    public async Task SeedOrg_Unnamed7Nodes_HaveNoIdentityAccount()
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppDbContext>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        var unnamedEmployees = await db.Employees
            .Where(e => e.ApplicationUserId == null)
            .ToListAsync();

        unnamedEmployees.Count.ShouldBe(7);

        // Hesapsız düğümlerin hiçbirinin e-postası UserManager'da olmamalı.
        foreach (var emp in unnamedEmployees)
        {
            if (emp.Email is not null)
            {
                var user = await userManager.FindByEmailAsync(emp.Email);
                user.ShouldBeNull($"{emp.Email} için UserManager'da hesap olmamalı");
            }
        }
    }

    // ---- İdempotency testi ----

    [Fact]
    public async Task SeedOrg_CalledTwice_EmployeeCountStaysAt14()
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppDbContext>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        // Seed zaten InitializeAsync içinde çalıştı; ikinci çağrı idempotent olmalı.
        await SeedOrgHierarchyAsync(db, userManager);

        var count = await db.Employees.CountAsync();
        count.ShouldBe(14);
    }

    [Fact]
    public async Task SeedOrg_CalledTwice_NoDuplicateUserAccounts()
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppDbContext>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        // İkinci seed çağrısı.
        await SeedOrgHierarchyAsync(db, userManager);

        // Her isimli e-posta için kullanıcı sayısı tam olarak 1 olmalı.
        var namedEmails = new[]
        {
            UmurEmail, AvniyeEmail, HalilEmail,
            MuhammedEmail, YigitEmail, IsmailEmail, SabanEmail
        };

        foreach (var email in namedEmails)
        {
            var users = userManager.Users.Where(u => u.Email == email).ToList();
            users.Count.ShouldBe(1, $"{email} için yalnızca 1 hesap olmalı (mükerrer yok)");
        }
    }

    // ---- Endpoint testi ----

    [Fact]
    public async Task GetEmployees_WithToken_Returns200And14Items()
    {
        var token = await LoginAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/employees");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponse<IReadOnlyList<EmployeeDto>>>(JsonOptions);

        payload.ShouldNotBeNull();
        payload!.Success.ShouldBeTrue();
        payload.Data.ShouldNotBeNull();
        payload.Data!.Count.ShouldBe(14);
    }

    [Fact]
    public async Task GetEmployees_WithToken_SubordinatesHaveManagerName()
    {
        var token = await LoginAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/employees");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponse<IReadOnlyList<EmployeeDto>>>(JsonOptions);
        var employees = payload!.Data!;

        // Umur dışındaki herkes yönetici adına sahip olmalı.
        var nonRoot = employees.Where(e => e.Email != UmurEmail).ToList();
        nonRoot.ShouldAllBe(e => e.ManagerName != null,
            "Umur haricindeki tüm personel için managerName dolu olmalı");

        // Umur'un yöneticisi yok.
        var umurDto = employees.SingleOrDefault(e => e.Email == UmurEmail);
        umurDto.ShouldNotBeNull();
        umurDto!.ManagerName.ShouldBeNull();
    }

    [Fact]
    public async Task GetEmployees_WithToken_NamedEmployeesHaveCorrectRoles()
    {
        var token = await LoginAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/employees");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponse<IReadOnlyList<EmployeeDto>>>(JsonOptions);
        var employees = payload!.Data!;

        var adminEmails = new[] { UmurEmail, AvniyeEmail, HalilEmail };
        foreach (var email in adminEmails)
        {
            var dto = employees.SingleOrDefault(e => e.Email == email);
            dto.ShouldNotBeNull($"{email} DTO listede bulunmalı");
            dto!.HasAccount.ShouldBeTrue($"{email} HasAccount=true olmalı");
            dto.Role.ShouldBe("Admin", $"{email} için rol Admin olmalı");
        }

        var salesEmails = new[] { MuhammedEmail, YigitEmail, IsmailEmail, SabanEmail };
        foreach (var email in salesEmails)
        {
            var dto = employees.SingleOrDefault(e => e.Email == email);
            dto.ShouldNotBeNull($"{email} DTO listede bulunmalı");
            dto!.HasAccount.ShouldBeTrue($"{email} HasAccount=true olmalı");
            dto.Role.ShouldBe("Sales", $"{email} için rol Sales olmalı");
        }
    }

    [Fact]
    public async Task GetEmployees_WithToken_UnnamedNodesHaveNoAccountAndNullRole()
    {
        var token = await LoginAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/employees");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponse<IReadOnlyList<EmployeeDto>>>(JsonOptions);
        var employees = payload!.Data!;

        var unnamedNodes = employees.Where(e => !e.HasAccount).ToList();
        unnamedNodes.Count.ShouldBe(7);
        unnamedNodes.ShouldAllBe(e => e.Role == null,
            "Hesapsız düğümlerin Role değeri null olmalı");
    }

    [Fact]
    public async Task GetEmployees_WithoutToken_Returns401()
    {
        // Token olmaksızın istek gönder.
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/employees");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
