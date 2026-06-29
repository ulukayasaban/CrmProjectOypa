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
using Oypa.Crm.Infrastructure.Identity;
using Oypa.Crm.Infrastructure.Persistence;
using Shouldly;

namespace Oypa.Crm.IntegrationTests;

/// <summary>
/// GET /api/auth/users ve DELETE /api/auth/users/{id} uçlarının integration testleri.
/// Admin yetki gerekliliği, kendini silme engeli ve başarılı silme akışı kapsanır.
/// </summary>
public sealed class UserManagementTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string SalesEmail = "um-sales@oypa.com.tr";
    private const string SalesPassword = "Sales!23456";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public UserManagementTests(CustomWebApplicationFactory factory)
    {
        _factory = factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                // Rate limiter testlerde devre dışı bırakılır
                services.PostConfigure<RateLimiterOptions>(options =>
                {
                    ClearRateLimiterPolicies(options);
                    foreach (var name in new[] { "auth-login", "auth-refresh", "urun-arama", "admin-sensitive" })
                        options.AddPolicy(name, _ => RateLimitPartition.GetNoLimiter("test"));
                });
            }));
        _client = _factory.CreateClient();
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
        await EnsureUserAsync(userManager, CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminPassword, "Test Yöneticisi", "Admin");
        await EnsureUserAsync(userManager, SalesEmail, SalesPassword, "Satış Kullanıcısı", "Sales");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -----------------------------------------------------------------------
    // Yardımcılar
    // -----------------------------------------------------------------------

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

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"{email} ile giriş başarısız");
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);
        return payload!.Data!.AccessToken;
    }

    private void SetBearer(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task<Guid> GetUserIdAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        return user!.Id;
    }

    private async Task<Guid> CreateDeleteableUserAsync(string email, string password = "TempUser!23456")
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            var existing = await userManager.FindByEmailAsync(email);
            return existing!.Id;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = "Silinecek Kullanıcı",
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(user, password);
        result.Succeeded.ShouldBeTrue();
        await userManager.AddToRoleAsync(user, "Sales");
        return user.Id;
    }

    // -----------------------------------------------------------------------
    // 401 — Anonim erişim
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetUsers_Anonymous_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/auth/users");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteUser_Anonymous_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.DeleteAsync($"/api/auth/users/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // -----------------------------------------------------------------------
    // 403 — Sales kullanıcısı admin uçlarına erişemez
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetUsers_AsSalesUser_Returns403()
    {
        var token = await LoginAsync(SalesEmail, SalesPassword);
        SetBearer(token);

        var response = await _client.GetAsync("/api/auth/users");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteUser_AsSalesUser_Returns403()
    {
        var token = await LoginAsync(SalesEmail, SalesPassword);
        SetBearer(token);

        var response = await _client.DeleteAsync($"/api/auth/users/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // -----------------------------------------------------------------------
    // GET /api/auth/users — 200, liste içeriği
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetUsers_AsAdmin_Returns200WithUserList()
    {
        var token = await LoginAsync(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        SetBearer(token);

        var response = await _client.GetAsync("/api/auth/users");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<UserDto>>>(JsonOptions);
        payload!.Success.ShouldBeTrue();
        payload.Data.ShouldNotBeNull();
        // Admin kullanıcı listede görünmeli
        payload.Data!.ShouldContain(u => u.Email == CustomWebApplicationFactory.AdminEmail);
    }

    [Fact]
    public async Task GetUsers_AsAdmin_ReturnsDtosWithRoles()
    {
        var token = await LoginAsync(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        SetBearer(token);

        var response = await _client.GetAsync("/api/auth/users");
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<UserDto>>>(JsonOptions);

        // Her UserDto'nun Roles listesi dolu olmalı (Admin kullanıcısı en az bir role sahip)
        var admin = payload!.Data!.FirstOrDefault(u => u.Email == CustomWebApplicationFactory.AdminEmail);
        admin.ShouldNotBeNull();
        admin!.Roles.ShouldContain("Admin");
    }

    // -----------------------------------------------------------------------
    // DELETE /api/auth/users/{id} — başarılı silme
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteUser_AsAdmin_DeletesUserAndReturns200()
    {
        var token = await LoginAsync(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        SetBearer(token);

        // Silinecek geçici kullanıcı oluştur
        var deleteEmail = $"del-{Guid.NewGuid():N}@oypa.com.tr";
        var userId = await CreateDeleteableUserAsync(deleteEmail);

        var response = await _client.DeleteAsync($"/api/auth/users/{userId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(JsonOptions);
        payload!.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteUser_AfterDeletion_NotInUserList()
    {
        var token = await LoginAsync(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        SetBearer(token);

        // Silinecek kullanıcı
        var deleteEmail = $"gone-{Guid.NewGuid():N}@oypa.com.tr";
        var userId = await CreateDeleteableUserAsync(deleteEmail);

        await _client.DeleteAsync($"/api/auth/users/{userId}");

        // Kullanıcı listesinde artık görünmemeli
        var listResponse = await _client.GetAsync("/api/auth/users");
        var payload = await listResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<UserDto>>>(JsonOptions);
        payload!.Data!.ShouldNotContain(u => u.Email == deleteEmail);
    }

    // -----------------------------------------------------------------------
    // DELETE /api/auth/users/{id} — kendini silme engeli
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteUser_SelfDeletion_Returns403()
    {
        var token = await LoginAsync(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        SetBearer(token);

        // Kendi id'sini al
        var adminId = await GetUserIdAsync(CustomWebApplicationFactory.AdminEmail);

        var response = await _client.DeleteAsync($"/api/auth/users/{adminId}");

        // ForbiddenAppException → 403
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // -----------------------------------------------------------------------
    // DELETE /api/auth/users/{id} — bulunamayan kullanıcı
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteUser_NonExistentId_Returns404()
    {
        var token = await LoginAsync(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        SetBearer(token);

        var response = await _client.DeleteAsync($"/api/auth/users/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // -----------------------------------------------------------------------
    // A2: Register çakışma (409) testi
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var token = await LoginAsync(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        SetBearer(token);

        var email = $"dup-{Guid.NewGuid():N}@oypa.com.tr";
        var request = new RegisterUserRequest(email, "Admin!23456", "İlk Kullanıcı", "Sales");

        // İlk kayıt başarılı
        var first = await _client.PostAsJsonAsync("/api/auth/register", request, JsonOptions);
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Aynı e-posta ile ikinci kayıt 409 dönmeli
        var second = await _client.PostAsJsonAsync("/api/auth/register", request, JsonOptions);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // -----------------------------------------------------------------------
    // A2: Kendini silme engeli testi (DeleteUser zaten var; açık adlandırma)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteUser_Self_Returns403()
    {
        var token = await LoginAsync(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        SetBearer(token);

        var adminId = await GetUserIdAsync(CustomWebApplicationFactory.AdminEmail);

        var response = await _client.DeleteAsync($"/api/auth/users/{adminId}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // -----------------------------------------------------------------------
    // A2: PUT /api/auth/users/{id}/role testleri
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ChangeUserRole_AsAdmin_Returns200_AndRoleApplies()
    {
        var token = await LoginAsync(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        SetBearer(token);

        // Önce Sales rolünde bir kullanıcı oluştur
        var email = $"role-change-{Guid.NewGuid():N}@oypa.com.tr";
        var userId = await CreateDeleteableUserAsync(email); // Sales rolünde oluşturulur

        // Rolü Sales → Admin yap
        var roleRequest = new ChangeUserRoleRequest("Admin");
        var response = await _client.PutAsJsonAsync($"/api/auth/users/{userId}/role", roleRequest, JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(JsonOptions);
        payload!.Success.ShouldBeTrue();

        // Kullanıcı listesinde rolün değiştiğini doğrula
        var listResponse = await _client.GetAsync("/api/auth/users");
        var listPayload = await listResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<UserDto>>>(JsonOptions);
        var changedUser = listPayload!.Data!.FirstOrDefault(u => u.Id == userId);
        changedUser.ShouldNotBeNull();
        changedUser!.Roles.ShouldContain("Admin", "Rol değişikliği sonrası kullanıcı Admin rolünde olmalı");
        changedUser.Roles.ShouldNotContain("Sales", "Eski Sales rolü kaldırılmış olmalı");
    }

    [Fact]
    public async Task ChangeOwnRole_AsAdmin_Returns403()
    {
        var token = await LoginAsync(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        SetBearer(token);

        var adminId = await GetUserIdAsync(CustomWebApplicationFactory.AdminEmail);

        var roleRequest = new ChangeUserRoleRequest("Sales");
        var response = await _client.PutAsJsonAsync($"/api/auth/users/{adminId}/role", roleRequest, JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden,
            "Admin kendi rolünü değiştiremez; kendini kilitlemesini engelle");
    }

    [Fact]
    public async Task ChangeUserRole_AsNonAdmin_Returns403()
    {
        var token = await LoginAsync(SalesEmail, SalesPassword);
        SetBearer(token);

        var roleRequest = new ChangeUserRoleRequest("Admin");
        var response = await _client.PutAsJsonAsync($"/api/auth/users/{Guid.NewGuid()}/role", roleRequest, JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // -----------------------------------------------------------------------
    // A2: POST /api/auth/users/{id}/reset-password testleri
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ResetUserPassword_AsAdmin_Returns200_WithNewCredentials_OldPasswordFails()
    {
        var token = await LoginAsync(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        SetBearer(token);

        // Sıfırlanacak kullanıcıyı oluştur (bilinen parolayla)
        var email = $"pwd-reset-{Guid.NewGuid():N}@oypa.com.tr";
        const string originalPassword = "TempUser!23456";
        await CreateDeleteableUserAsync(email, originalPassword);

        // Kullanıcının id'sini al
        var userId = await GetUserIdAsync(email);

        // Parolayı sıfırla
        var response = await _client.PostAsync($"/api/auth/users/{userId}/reset-password", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AccountCredentialDto>>(JsonOptions);
        payload!.Success.ShouldBeTrue();
        payload.Data.ShouldNotBeNull();
        payload.Data!.Email.ShouldBe(email);
        var newPassword = payload.Data.TempPassword;
        newPassword.ShouldNotBeNullOrWhiteSpace();
        newPassword.ShouldNotBe(originalPassword, "Yeni parola eski paroladan farklı olmalı");
        newPassword.Length.ShouldBeGreaterThanOrEqualTo(12);

        // Eski parola artık çalışmamalı
        _client.DefaultRequestHeaders.Authorization = null;
        var oldLogin = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, originalPassword), JsonOptions);
        oldLogin.StatusCode.ShouldNotBe(HttpStatusCode.OK,
            "Eski parola sıfırlandıktan sonra giriş sağlamamalı");

        // Yeni parola çalışmalı
        var newLogin = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, newPassword), JsonOptions);
        newLogin.StatusCode.ShouldBe(HttpStatusCode.OK,
            "Yeni geçici parola ile giriş yapılabilmeli");
    }

    [Fact]
    public async Task ResetUserPassword_AsNonAdmin_Returns403()
    {
        var token = await LoginAsync(SalesEmail, SalesPassword);
        SetBearer(token);

        var response = await _client.PostAsync($"/api/auth/users/{Guid.NewGuid()}/reset-password", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // -----------------------------------------------------------------------
    // A2 (opsiyonel): Audit log — kullanıcı işlemi sonrası AuditLog kaydı doğrulama
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Register_AsAdmin_CreatesAuditLogEntry()
    {
        var token = await LoginAsync(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        SetBearer(token);

        var email = $"audit-test-{Guid.NewGuid():N}@oypa.com.tr";
        var request = new RegisterUserRequest(email, "Admin!23456", "Audit Test Kullanıcı", "Sales");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request, JsonOptions);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        // AuditLogs tablosunda bu kullanıcı için kayıt oluşmalı
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var auditLogs = await db.AuditLogs
            .Where(a => a.EntityName == "ApplicationUser" && a.Action == Oypa.Crm.Domain.Enums.AuditAction.Created)
            .ToListAsync();

        auditLogs.ShouldNotBeEmpty("Kullanıcı oluşturma sonrası AuditLog kaydı oluşmalı");
    }
}
