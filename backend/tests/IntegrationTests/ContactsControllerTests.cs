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
using Microsoft.Extensions.DependencyInjection;
using Oypa.Crm.Contracts.Auth;
using Oypa.Crm.Contracts.Common;
using Oypa.Crm.Contracts.Companies;
using Oypa.Crm.Contracts.Contacts;
using Oypa.Crm.Domain.Enums;
using Oypa.Crm.Infrastructure.Identity;
using Oypa.Crm.Infrastructure.Persistence;
using Shouldly;

namespace Oypa.Crm.IntegrationTests;

/// <summary>
/// Contact CRUD uçlarının uçtan uca integration testleri.
/// Mevcut şirket üzerinden contact oluşturma, sorgulama, güncelleme ve silme senaryoları kapsanır.
/// </summary>
public sealed class ContactsControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public ContactsControllerTests(CustomWebApplicationFactory factory)
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

    private async Task<string> LoginAdminAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);
        return payload!.Data!.AccessToken;
    }

    private void SetBearer(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task<Guid> CreateCompanyAsync(string token)
    {
        SetBearer(token);
        var name = $"İletişim Test Firması {Guid.NewGuid():N}";
        var response = await _client.PostAsJsonAsync("/api/companies",
            new CreateCompanyRequest(name, Sector.Retail, "0212", "test@company.com", "Test Adres"), JsonOptions);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>(JsonOptions);
        return payload!.Data!.Id;
    }

    private async Task<Guid> AddContactAsync(Guid companyId, string name)
    {
        var response = await _client.PostAsJsonAsync($"/api/companies/{companyId}/contacts",
            new CreateContactRequest(name, $"{name.Replace(" ", "")}@test.com", "555"), JsonOptions);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<ContactDto>>(JsonOptions);
        return payload!.Data!.Id;
    }

    // -----------------------------------------------------------------------
    // 401 — Anonim erişim
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetContactById_Anonymous_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/api/companies/contacts/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateContact_Anonymous_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PutAsJsonAsync($"/api/companies/contacts/{Guid.NewGuid()}",
            new UpdateContactRequest("Ad", null, null), JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteContact_Anonymous_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.DeleteAsync($"/api/companies/contacts/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // -----------------------------------------------------------------------
    // GET /api/companies/contacts/{contactId} — 200 ve 404
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetContactById_ExistingContact_Returns200WithDto()
    {
        var token = await LoginAdminAsync();
        var companyId = await CreateCompanyAsync(token);
        var contactId = await AddContactAsync(companyId, $"Deneme Kişi {Guid.NewGuid():N}");

        var response = await _client.GetAsync($"/api/companies/contacts/{contactId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<ContactDto>>(JsonOptions);
        payload!.Success.ShouldBeTrue();
        payload.Data!.Id.ShouldBe(contactId);
    }

    [Fact]
    public async Task GetContactById_NonExistent_Returns404()
    {
        var token = await LoginAdminAsync();
        SetBearer(token);

        var response = await _client.GetAsync($"/api/companies/contacts/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // -----------------------------------------------------------------------
    // PUT /api/companies/contacts/{contactId} — 200 ve 404
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateContact_ExistingContact_Returns200WithUpdatedData()
    {
        var token = await LoginAdminAsync();
        var companyId = await CreateCompanyAsync(token);
        var contactId = await AddContactAsync(companyId, $"Güncellenecek {Guid.NewGuid():N}");

        var uniqueName = $"Güncellenmiş Kişi {Guid.NewGuid():N}";
        var response = await _client.PutAsJsonAsync($"/api/companies/contacts/{contactId}",
            new UpdateContactRequest(uniqueName, "guncellendi@test.com", "777"), JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<ContactDto>>(JsonOptions);
        payload!.Data!.Name.ShouldBe(uniqueName);
        payload.Data.Email.ShouldBe("guncellendi@test.com");
        payload.Data.Phone.ShouldBe("777");
    }

    [Fact]
    public async Task UpdateContact_NonExistent_Returns404()
    {
        var token = await LoginAdminAsync();
        SetBearer(token);

        var response = await _client.PutAsJsonAsync($"/api/companies/contacts/{Guid.NewGuid()}",
            new UpdateContactRequest("Ad", null, null), JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // -----------------------------------------------------------------------
    // DELETE /api/companies/contacts/{contactId} — 200 ve 404
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteContact_ExistingContact_Returns200()
    {
        var token = await LoginAdminAsync();
        var companyId = await CreateCompanyAsync(token);
        var contactId = await AddContactAsync(companyId, $"Silinecek Kişi {Guid.NewGuid():N}");

        var response = await _client.DeleteAsync($"/api/companies/contacts/{contactId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(JsonOptions);
        payload!.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteContact_AfterDeletion_Returns404OnGet()
    {
        var token = await LoginAdminAsync();
        var companyId = await CreateCompanyAsync(token);
        var contactId = await AddContactAsync(companyId, $"Sil ve Doğrula {Guid.NewGuid():N}");

        // Sil
        var deleteResponse = await _client.DeleteAsync($"/api/companies/contacts/{contactId}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Tekrar sorgula → 404 olmalı
        var getResponse = await _client.GetAsync($"/api/companies/contacts/{contactId}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteContact_NonExistent_Returns404()
    {
        var token = await LoginAdminAsync();
        SetBearer(token);

        var response = await _client.DeleteAsync($"/api/companies/contacts/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
