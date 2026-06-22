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
/// Personel yönetimi (kapsam, CRUD, rol/parola/hesap) uçtan uca testleri.
/// OrgSeedTests deseni izlenerek, seed sonrası gerçek JWT ile kapsam doğrulanır.
/// </summary>
public sealed class EmployeeManagementTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    // Seed'de tanımlı e-posta adresleri
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

    public EmployeeManagementTests(CustomWebApplicationFactory factory)
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

    /// <summary>Roller, admin kullanıcı ve organizasyon hiyerarşisini seed eder.</summary>
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

        var yigit = new Employee("Satış Uzmanı", "Yiğit ERSOY", YigitEmail, avniye.Id);
        await LinkOrgAccountAsync(yigit, YigitEmail, "Yiğit ERSOY", OrgPassword, "Sales", userManager);

        var ismail = new Employee("Depo Şefi", "İsmail TAZECAN", IsmailEmail, avniye.Id);
        await LinkOrgAccountAsync(ismail, IsmailEmail, "İsmail TAZECAN", OrgPassword, "Sales", userManager);

        var turizmYon = new Employee("Turizm Satış ve İş Geliştirme Yöneticisi", managerId: avniye.Id);
        var satiOpsYon = new Employee("Satış Operasyonları ve Raporlama Yöneticisi", managerId: avniye.Id);
        var tesisYon = new Employee("Tesis Satış ve İş Geliştirme Yöneticisi", managerId: avniye.Id);
        var perakendeYon = new Employee("Perakende Satış Yöneticisi", managerId: avniye.Id);

        var pazarArastirma = new Employee("Pazar Araştırma ve Analiz Yöneticisi", managerId: halil.Id);
        var pazarGelistirme = new Employee("Pazar Geliştirme ve Fiyatlandırma Yöneticisi", managerId: halil.Id);

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
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"{email} ile giriş başarısız olmamalı");
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);
        return payload!.Data!.AccessToken;
    }

    private void SetBearer(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // -----------------------------------------------------------------------
    // Kapsam testleri
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetManaged_AsUmur_ReturnsAll14Employees()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(token);

        // Act
        var response = await _client.GetAsync("/api/employees/managed");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<EmployeeDto>>>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Success.ShouldBeTrue();
        payload.Data!.Count.ShouldBe(14,
            "Kök düğüm olan Umur'un kapsamı tüm 14 personeli içermeli");
    }

    [Fact]
    public async Task GetManaged_AsAvniye_ReturnsOnlyAvniyeSubtree()
    {
        // Arrange — Avniye'nin alt-ağacı: kendisi + Muhammed, Yiğit, İsmail + 4 hesapsız = 8
        var token = await LoginAndGetTokenAsync(AvniyeEmail, OrgPassword);
        SetBearer(token);

        // Act
        var response = await _client.GetAsync("/api/employees/managed");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<EmployeeDto>>>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Success.ShouldBeTrue();

        var ids = payload.Data!.Select(e => e.Email).ToHashSet();

        // Kendi kümesindekiler görünmeli
        ids.ShouldContain(AvniyeEmail, "Avniye kendi kapsamında görünmeli");
        ids.ShouldContain(MuhammedEmail, "Muhammed Avniye kapsamında");
        ids.ShouldContain(YigitEmail, "Yiğit Avniye kapsamında");
        ids.ShouldContain(IsmailEmail, "İsmail Avniye kapsamında");

        // Halil'in ekibi görünmemeli
        ids.ShouldNotContain(HalilEmail, "Halil Avniye kapsamının dışında");

        // Umur'un tüm ağacı değil, kendi ağacı
        payload.Data!.Count.ShouldBe(8,
            "Avniye kapsamı: kendisi + Muhammed + Yiğit + İsmail + 4 hesapsız = 8");
    }

    [Fact]
    public async Task GetManaged_AsAvniye_CannotAccessHalilsSubordinate()
    {
        // Arrange
        var umurToken = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(umurToken);
        var allResponse = await _client.GetAsync("/api/employees");
        var allPayload = await allResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<EmployeeDto>>>(JsonOptions);
        var halilEmployee = allPayload!.Data!.Single(e => e.Email == HalilEmail);

        // Halil'e bağlı hesapsız bir çalışan bul
        var halilSubordinate = allPayload.Data!
            .FirstOrDefault(e => e.ManagerId == halilEmployee.Id && !e.HasAccount);
        halilSubordinate.ShouldNotBeNull("Halil'in en az 1 hesapsız astı olmalı");

        // Avniye tokenı ile o çalışana PUT yapılırsa 403 beklenir
        var avniyeToken = await LoginAndGetTokenAsync(AvniyeEmail, OrgPassword);
        SetBearer(avniyeToken);

        var updateRequest = new UpdateEmployeeRequest("Güncellendi", null, null);
        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/employees/{halilSubordinate!.Id}", updateRequest, JsonOptions);

        // Assert
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden,
            "Avniye, Halil'in astını güncelleyemez (kapsam dışı)");
    }

    [Fact]
    public async Task AssignRole_AsAvniye_ToHalilSubordinate_Returns403()
    {
        // Arrange — Halil'in bir astını bul
        var umurToken = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(umurToken);
        var allResponse = await _client.GetAsync("/api/employees");
        var allPayload = await allResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<EmployeeDto>>>(JsonOptions);
        var halil = allPayload!.Data!.Single(e => e.Email == HalilEmail);

        // Halil'in bir astını seç (hesapsız düğüm)
        var halilSub = allPayload.Data!.FirstOrDefault(e => e.ManagerId == halil.Id);
        halilSub.ShouldNotBeNull();

        // Avniye token'ı ile rol atamaya çalış
        var avniyeToken = await LoginAndGetTokenAsync(AvniyeEmail, OrgPassword);
        SetBearer(avniyeToken);

        var response = await _client.PutAsJsonAsync(
            $"/api/employees/{halilSub!.Id}/role",
            new AssignRoleRequest("Sales"),
            JsonOptions);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden,
            "Avniye Halil'in astına rol atayamaz");
    }

    [Fact]
    public async Task GetManaged_AsSalesUser_ReturnsSelfOnlySubtree()
    {
        // Muhammed bir Sales kullanıcısı; astı yok.
        // ResolveScopeAsync: ManagerId != null → subtree hesapla → yalnız kendisi (1 kişi).
        // count == 0 olmadığından 200 dönmeli, listede sadece kendisi.
        var token = await LoginAndGetTokenAsync(MuhammedEmail, OrgPassword);
        SetBearer(token);

        var response = await _client.GetAsync("/api/employees/managed");

        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            "Astı olmayan Sales kullanıcısı kendi subtree'sini (kendisi) görebilmeli");
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<EmployeeDto>>>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Data!.Count.ShouldBe(1,
            "Astı olmayan Sales kullanıcısının kapsamında yalnız kendisi bulunmalı");
        payload.Data[0].Email.ShouldBe(MuhammedEmail);
    }

    [Fact]
    public async Task CreateEmployee_AsSalesUser_Returns403BecauseNewEmployeeOutsideScope()
    {
        // Sales kullanıcısı (Muhammed) personel oluşturmaya çalışır.
        // ManagerId belirtilmezse kapsam kontrolü atlanır → personel oluşturulur.
        // Ancak Halil'e bağlı personel oluşturmak kapsam dışı → 403 beklenir.
        var umurToken = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(umurToken);
        var allResponse = await _client.GetAsync("/api/employees");
        var allPayload = await allResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<EmployeeDto>>>(JsonOptions);
        var halil = allPayload!.Data!.Single(e => e.Email == HalilEmail);

        var muhammedToken = await LoginAndGetTokenAsync(MuhammedEmail, OrgPassword);
        SetBearer(muhammedToken);

        // Halil'e bağlı personel oluşturmak kapsam dışı olduğundan 403 beklenir
        var request = new CreateEmployeeRequest("Yeni Ünvan", "Yeni İsim", null, halil.Id, false, null);
        var response = await _client.PostAsJsonAsync("/api/employees", request, JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden,
            "Muhammed Halil'e bağlı personel oluşturamaz (kapsam dışı)");
    }

    // -----------------------------------------------------------------------
    // CRUD testleri
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateEmployee_WithoutAccount_AsUmur_Returns201WithEmployeeDto()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(token);

        var request = new CreateEmployeeRequest(
            Title: "Test Ünvanı",
            FullName: "Test Personel",
            Email: "test.personel@oypa.com.tr",
            ManagerId: null,
            CreateAccount: false,
            Role: null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/employees", request, JsonOptions);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<CreateEmployeeResult>>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Success.ShouldBeTrue();
        payload.Data!.Employee.ShouldNotBeNull();
        payload.Data.Employee.Title.ShouldBe("Test Ünvanı");
        payload.Data.Employee.FullName.ShouldBe("Test Personel");
        payload.Data.Account.ShouldBeNull("Hesap oluşturulmadı — Account null olmalı");
    }

    [Fact]
    public async Task CreateEmployee_WithAccount_AsUmur_Returns201WithAccountCredentials()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(token);

        var newEmail = $"yeni.satis.{Guid.NewGuid():N}@oypa.com.tr";
        var request = new CreateEmployeeRequest(
            Title: "Satış Uzmanı",
            FullName: "Yeni Satışçı",
            Email: newEmail,
            ManagerId: null,
            CreateAccount: true,
            Role: "Sales");

        // Act
        var response = await _client.PostAsJsonAsync("/api/employees", request, JsonOptions);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<CreateEmployeeResult>>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Success.ShouldBeTrue();
        payload.Data!.Employee.HasAccount.ShouldBeTrue("Hesap oluşturulduğunda HasAccount=true olmalı");
        payload.Data.Account.ShouldNotBeNull("Hesap oluşturulduğunda Account dolu olmalı");
        payload.Data.Account!.Email.ShouldBe(newEmail);
        payload.Data.Account.TempPassword.ShouldNotBeNullOrWhiteSpace();
        payload.Data.Account.TempPassword.Length.ShouldBeGreaterThanOrEqualTo(12,
            "Geçici parola en az 12 karakter olmalı");
    }

    [Fact]
    public async Task CreateEmployee_WithAccount_NewUserCanLogin()
    {
        // Arrange — hesapla personel oluştur
        var umurToken = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(umurToken);

        var newEmail = $"login.test.{Guid.NewGuid():N}@oypa.com.tr";
        var createRequest = new CreateEmployeeRequest(
            Title: "Test Ünvanı",
            FullName: "Login Test",
            Email: newEmail,
            ManagerId: null,
            CreateAccount: true,
            Role: "Sales");

        var createResponse = await _client.PostAsJsonAsync("/api/employees", createRequest, JsonOptions);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var createPayload = await createResponse.Content.ReadFromJsonAsync<ApiResponse<CreateEmployeeResult>>(JsonOptions);
        var tempPassword = createPayload!.Data!.Account!.TempPassword;

        // Act — yeni hesapla giriş yap
        _client.DefaultRequestHeaders.Authorization = null;
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(newEmail, tempPassword), JsonOptions);

        // Assert
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.OK,
            "Geçici parola ile giriş yapılabilmeli");
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);
        loginPayload!.Data!.AccessToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task UpdateEmployee_AsUmur_Returns200WithUpdatedData()
    {
        // Arrange — yeni personel oluştur, ardından güncelle
        var token = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(token);

        var createRequest = new CreateEmployeeRequest(
            "Eski Ünvan", "Eski İsim", null, null, false, null);
        var createResponse = await _client.PostAsJsonAsync("/api/employees", createRequest, JsonOptions);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<CreateEmployeeResult>>(JsonOptions))!.Data!.Employee;

        // Act
        var updateRequest = new UpdateEmployeeRequest("Yeni Ünvan", "Yeni İsim", "yeni@oypa.com.tr");
        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/employees/{created.Id}", updateRequest, JsonOptions);

        // Assert
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updatePayload = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<EmployeeDto>>(JsonOptions);
        updatePayload!.Data!.Title.ShouldBe("Yeni Ünvan");
        updatePayload.Data.FullName.ShouldBe("Yeni İsim");
        updatePayload.Data.Email.ShouldBe("yeni@oypa.com.tr");
    }

    [Fact]
    public async Task DeleteEmployee_WithSubordinates_Returns409()
    {
        // Arrange — Avniye'nin astları var; Avniye'yi silmeye çalış
        var token = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(token);

        var allResponse = await _client.GetAsync("/api/employees");
        var allPayload = await allResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<EmployeeDto>>>(JsonOptions);
        var avniye = allPayload!.Data!.Single(e => e.Email == AvniyeEmail);

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/employees/{avniye.Id}");

        // Assert
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict,
            "Astı olan personel silinemez → 409 Conflict");
    }

    [Fact]
    public async Task DeleteEmployee_WithoutSubordinates_Returns200()
    {
        // Arrange — hesapsız yeni personel oluştur (astı olmayacak)
        var token = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(token);

        var createRequest = new CreateEmployeeRequest(
            "Silinecek Ünvan", "Silinecek İsim", null, null, false, null);
        var createResponse = await _client.PostAsJsonAsync("/api/employees", createRequest, JsonOptions);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<CreateEmployeeResult>>(JsonOptions))!.Data!.Employee;

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/employees/{created.Id}");

        // Assert
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK,
            "Astı olmayan personel silinebilir");
    }

    [Fact]
    public async Task AssignManager_SelfAssignment_ReturnsConflict()
    {
        // Arrange — Umur kendi kendine yönetici atanamaz
        var token = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(token);

        var allResponse = await _client.GetAsync("/api/employees");
        var allPayload = await allResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<EmployeeDto>>>(JsonOptions);
        var umur = allPayload!.Data!.Single(e => e.Email == UmurEmail);

        // Act — Umur'u kendi astına (kendine) bağla
        var request = new AssignManagerRequest(umur.Id);
        var response = await _client.PutAsJsonAsync($"/api/employees/{umur.Id}/manager", request, JsonOptions);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict,
            "Personel kendi yöneticisi olamaz → 409");
    }

    [Fact]
    public async Task AssignManager_CyclicAssignment_ReturnsConflict()
    {
        // Arrange — Umur → Avniye hiyerarşisinde, Umur'u Avniye'ye bağlamak döngü oluşturur
        var token = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(token);

        var allResponse = await _client.GetAsync("/api/employees");
        var allPayload = await allResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<EmployeeDto>>>(JsonOptions);
        var umur = allPayload!.Data!.Single(e => e.Email == UmurEmail);
        var avniye = allPayload.Data!.Single(e => e.Email == AvniyeEmail);

        // Act — Umur'u Avniye'ye bağla (Avniye zaten Umur'un altında → döngü)
        var request = new AssignManagerRequest(avniye.Id);
        var response = await _client.PutAsJsonAsync($"/api/employees/{umur.Id}/manager", request, JsonOptions);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict,
            "Alt-ağaca atama döngü oluşturur → 409");
    }

    // -----------------------------------------------------------------------
    // Hesap oluşturma testleri
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAccount_ForAccountlessEmployee_Returns201WithCredentials()
    {
        // Arrange — hesapsız bir personel oluştur
        var token = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(token);

        var personelEmail = $"hesapsiz.{Guid.NewGuid():N}@oypa.com.tr";
        var createEmpRequest = new CreateEmployeeRequest(
            "Hesapsız Ünvan", "Hesapsız Personel", personelEmail, null, false, null);
        var createEmpResponse = await _client.PostAsJsonAsync("/api/employees", createEmpRequest, JsonOptions);
        createEmpResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = (await createEmpResponse.Content.ReadFromJsonAsync<ApiResponse<CreateEmployeeResult>>(JsonOptions))!.Data!.Employee;

        // Act — hesap oluştur
        var accountRequest = new CreateAccountRequest("Sales");
        var accountResponse = await _client.PostAsJsonAsync(
            $"/api/employees/{created.Id}/account", accountRequest, JsonOptions);

        // Assert
        accountResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var accountPayload = await accountResponse.Content.ReadFromJsonAsync<ApiResponse<AccountCredentialDto>>(JsonOptions);
        accountPayload.ShouldNotBeNull();
        accountPayload!.Success.ShouldBeTrue();
        accountPayload.Data!.Email.ShouldBe(personelEmail);
        accountPayload.Data.TempPassword.ShouldNotBeNullOrWhiteSpace();
        accountPayload.Data.TempPassword.Length.ShouldBeGreaterThanOrEqualTo(12);
    }

    // -----------------------------------------------------------------------
    // Rol/parola testleri
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AssignRole_SalestoAdmin_AsUmur_RoleChangesOnNextLogin()
    {
        // Arrange — yeni Sales personel oluştur
        var umurToken = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(umurToken);

        var personelEmail = $"rol.degisim.{Guid.NewGuid():N}@oypa.com.tr";
        var createRequest = new CreateEmployeeRequest(
            "Rol Test Ünvanı", "Rol Test", personelEmail, null, true, "Sales");
        var createResponse = await _client.PostAsJsonAsync("/api/employees", createRequest, JsonOptions);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var createPayload = await createResponse.Content.ReadFromJsonAsync<ApiResponse<CreateEmployeeResult>>(JsonOptions);
        var employee = createPayload!.Data!.Employee;
        var tempPassword = createPayload.Data.Account!.TempPassword;

        // Act — rolü Sales → Admin yap
        var roleRequest = new AssignRoleRequest("Admin");
        var roleResponse = await _client.PutAsJsonAsync(
            $"/api/employees/{employee.Id}/role", roleRequest, JsonOptions);

        // Assert
        roleResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var rolePayload = await roleResponse.Content.ReadFromJsonAsync<ApiResponse<EmployeeDto>>(JsonOptions);
        rolePayload!.Success.ShouldBeTrue();

        // Yeni token ile giriş yapıp rol bilgisini GET /api/employees ile doğrula
        _client.DefaultRequestHeaders.Authorization = null;
        var newLoginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(personelEmail, tempPassword), JsonOptions);
        newLoginResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var newToken = (await newLoginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions))!.Data!.AccessToken;
        SetBearer(umurToken); // Umur'un tokenı ile listeye bak (Sales user göremeyebilir)

        var allResponse = await _client.GetAsync("/api/employees");
        var allPayload = await allResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<EmployeeDto>>>(JsonOptions);
        var updatedEmployee = allPayload!.Data!.SingleOrDefault(e => e.Id == employee.Id);
        updatedEmployee.ShouldNotBeNull();
        updatedEmployee!.Role.ShouldBe("Admin",
            "Rol atandıktan sonra EmployeeDto.Role = Admin olmalı");
    }

    [Fact]
    public async Task ResetPassword_AsUmur_OldPasswordNoLongerWorks()
    {
        // Arrange — yeni hesaplı personel oluştur
        var umurToken = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(umurToken);

        var personelEmail = $"sifre.reset.{Guid.NewGuid():N}@oypa.com.tr";
        var createRequest = new CreateEmployeeRequest(
            "Şifre Test", "Şifre Test", personelEmail, null, true, "Sales");
        var createResponse = await _client.PostAsJsonAsync("/api/employees", createRequest, JsonOptions);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var createPayload = await createResponse.Content.ReadFromJsonAsync<ApiResponse<CreateEmployeeResult>>(JsonOptions);
        var employee = createPayload!.Data!.Employee;
        var originalPassword = createPayload.Data.Account!.TempPassword;

        // Act — parolayı sıfırla
        var resetResponse = await _client.PostAsync(
            $"/api/employees/{employee.Id}/reset-password", null);

        // Assert
        resetResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var resetPayload = await resetResponse.Content.ReadFromJsonAsync<ApiResponse<AccountCredentialDto>>(JsonOptions);
        resetPayload!.Success.ShouldBeTrue();
        var newPassword = resetPayload.Data!.TempPassword;
        newPassword.ShouldNotBeNullOrWhiteSpace();
        newPassword.ShouldNotBe(originalPassword, "Yeni parola eski paroladan farklı olmalı");
        newPassword.Length.ShouldBeGreaterThanOrEqualTo(12);

        // Eski parola artık çalışmamalı
        _client.DefaultRequestHeaders.Authorization = null;
        var oldPasswordLogin = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(personelEmail, originalPassword), JsonOptions);
        oldPasswordLogin.StatusCode.ShouldNotBe(HttpStatusCode.OK,
            "Eski parola sıfırlandıktan sonra giriş sağlamamalı");

        // Yeni parola çalışmalı
        var newPasswordLogin = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(personelEmail, newPassword), JsonOptions);
        newPasswordLogin.StatusCode.ShouldBe(HttpStatusCode.OK,
            "Yeni parola ile giriş yapılabilmeli");
    }

    [Fact]
    public async Task UnlinkAccount_AsUmur_HasAccountBecomeFalse()
    {
        // Arrange — yeni hesaplı personel oluştur
        var umurToken = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(umurToken);

        var personelEmail = $"unlink.test.{Guid.NewGuid():N}@oypa.com.tr";
        var createRequest = new CreateEmployeeRequest(
            "Unlink Test", "Unlink Test", personelEmail, null, true, "Sales");
        var createResponse = await _client.PostAsJsonAsync("/api/employees", createRequest, JsonOptions);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var employee = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<CreateEmployeeResult>>(JsonOptions))!.Data!.Employee;
        employee.HasAccount.ShouldBeTrue("Oluşturulan personelin hesabı olmalı");

        // Act — hesap bağlantısını kaldır
        var unlinkResponse = await _client.DeleteAsync($"/api/employees/{employee.Id}/account");

        // Assert
        unlinkResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var unlinkPayload = await unlinkResponse.Content.ReadFromJsonAsync<ApiResponse<EmployeeDto>>(JsonOptions);
        unlinkPayload!.Success.ShouldBeTrue();
        unlinkPayload.Data!.HasAccount.ShouldBeFalse(
            "Hesap bağlantısı kaldırıldıktan sonra HasAccount=false olmalı");
    }

    [Fact]
    public async Task GeneratedPassword_MeetsComplexityPolicy()
    {
        // Arrange — hesaplı personel oluştur ve dönen geçici parolayı doğrula
        var token = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(token);

        var personelEmail = $"policy.test.{Guid.NewGuid():N}@oypa.com.tr";
        var createRequest = new CreateEmployeeRequest(
            "Politika Test", "Politika Test", personelEmail, null, true, "Sales");
        var createResponse = await _client.PostAsJsonAsync("/api/employees", createRequest, JsonOptions);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var tempPassword = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<CreateEmployeeResult>>(JsonOptions))!.Data!.Account!.TempPassword;

        // Assert — parola politika gereksinimlerini karşılamalı
        tempPassword.Length.ShouldBeGreaterThanOrEqualTo(12, "En az 12 karakter");
        tempPassword.Any(char.IsUpper).ShouldBeTrue("En az 1 büyük harf");
        tempPassword.Any(char.IsLower).ShouldBeTrue("En az 1 küçük harf");
        tempPassword.Any(char.IsDigit).ShouldBeTrue("En az 1 rakam");
        tempPassword.Any(c => "!@#$%^&*".Contains(c)).ShouldBeTrue("En az 1 özel karakter");
    }

    // -----------------------------------------------------------------------
    // Uçtan uca akış
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FullFlow_CreateUpdateDeleteEmployee_WorksEndToEnd()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync(UmurEmail, OrgPassword);
        SetBearer(token);

        // 1) Oluştur
        var createRequest = new CreateEmployeeRequest(
            "Akış Testi Ünvanı", "Akış Testi", "akis.testi@oypa.com.tr", null, false, null);
        var createResponse = await _client.PostAsJsonAsync("/api/employees", createRequest, JsonOptions);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var employee = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<CreateEmployeeResult>>(JsonOptions))!.Data!.Employee;

        // 2) Güncelle
        var updateRequest = new UpdateEmployeeRequest("Güncel Ünvan", "Güncel İsim", "guncellendi@oypa.com.tr");
        var updateResponse = await _client.PutAsJsonAsync($"/api/employees/{employee.Id}", updateRequest, JsonOptions);
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = (await updateResponse.Content.ReadFromJsonAsync<ApiResponse<EmployeeDto>>(JsonOptions))!.Data!;
        updated.Title.ShouldBe("Güncel Ünvan");

        // 3) GET /managed ile varlığını doğrula (GetById endpoint yok; managed kapsamda görünmeli)
        var managedResponse = await _client.GetAsync("/api/employees/managed");
        managedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var managedPayload = await managedResponse.Content
            .ReadFromJsonAsync<ApiResponse<IReadOnlyList<EmployeeDto>>>(JsonOptions);
        managedPayload!.Data!.ShouldContain(e => e.Id == employee.Id,
            "Güncel personel managed listesinde görünmeli");

        // 4) Sil
        var deleteResponse = await _client.DeleteAsync($"/api/employees/{employee.Id}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetManagedList_AsAvniye_DoesNotContainHalil()
    {
        // Avniye'nin managed listesi Halil'i içermemeli (kapsam dışı)
        var avniyeToken = await LoginAndGetTokenAsync(AvniyeEmail, OrgPassword);
        SetBearer(avniyeToken);

        var response = await _client.GetAsync("/api/employees/managed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<EmployeeDto>>>(JsonOptions);

        payload!.Data!.ShouldNotContain(e => e.Email == HalilEmail,
            "Halil Avniye'nin kapsamının dışında; managed listesinde görünmemeli");
    }

    [Fact]
    public async Task GetManaged_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/employees/managed");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/employees");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
