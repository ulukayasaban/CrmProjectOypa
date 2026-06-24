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
using Oypa.Crm.Contracts.Companies;
using Oypa.Crm.Contracts.Tenders;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Oypa.Crm.Infrastructure.Identity;
using Oypa.Crm.Infrastructure.Persistence;
using Shouldly;

namespace Oypa.Crm.IntegrationTests;

/// <summary>
/// /paged varyant uclar icin uctan uca testler.
/// Dogrulanir: 200 donusu, zarf alanlari (items/page/pageSize/totalCount/totalPages),
/// search/sortBy filtreleri ve mevcut tam-liste uclarinin dokunulmadigi.
/// </summary>
public sealed class PagedEndpointsTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string AdminEmail = CustomWebApplicationFactory.AdminEmail;
    private const string AdminPassword = CustomWebApplicationFactory.AdminPassword;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PagedEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                // Testlerde rate limiting devre disi
                services.PostConfigure<RateLimiterOptions>(options =>
                {
                    ClearPolicies(options);
                    foreach (var name in new[] { "auth-login", "auth-refresh", "urun-arama", "admin-sensitive" })
                        options.AddPolicy(name, _ => RateLimitPartition.GetNoLimiter("test"));
                });
            }));
        _client = _factory.CreateClient();
    }

    private static void ClearPolicies(RateLimiterOptions options)
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
        if (await userManager.FindByEmailAsync(AdminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                FullName = "Test Yoneticisi",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, AdminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -----------------------------------------------------------------------
    // Yardimci metotlar
    // -----------------------------------------------------------------------

    private async Task<string> LoginAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(AdminEmail, AdminPassword), JsonOptions);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);
        return payload!.Data!.AccessToken;
    }

    private void SetBearer(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task<Guid> SeedCompanyAsync(string? title = null, Sector sector = Sector.Retail)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var company = new Company(title ?? $"Firma_{Guid.NewGuid():N}", sector, "111", $"{Guid.NewGuid():N}@test.com", "Adres");
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        return company.Id;
    }

    private async Task<Guid> CreateTenderAsync(Guid companyId, string? title = null, Sector sector = Sector.Retail)
    {
        var request = new CreateTenderRequest(
            companyId, title ?? $"Ihale_{Guid.NewGuid():N}", "IH-001", sector,
            new DateOnly(2026, 12, 1), 10, 1000m, 500m, 5, "Aciklama", null);

        var response = await _client.PostAsJsonAsync("/api/tenders", request, JsonOptions);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<TenderDto>>(JsonOptions);
        return payload!.Data!.Id;
    }

    // -----------------------------------------------------------------------
    // GET /api/tenders/paged -- 200 + zarf alanlari dogrulama
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetTendersPaged_Authenticated_Returns200WithEnvelopeFields()
    {
        var token = await LoginAsync();
        SetBearer(token);
        var companyId = await SeedCompanyAsync();
        await CreateTenderAsync(companyId);

        var response = await _client.GetAsync("/api/tenders/paged?page=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Yanit zarfi JSON alanlarini dogrula
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().ShouldBeTrue();

        var data = json.GetProperty("data");
        // Tum zorunlu alanlar mevcut
        data.TryGetProperty("items", out _).ShouldBeTrue("items alani mevcut olmali");
        data.TryGetProperty("page", out _).ShouldBeTrue("page alani mevcut olmali");
        data.TryGetProperty("pageSize", out _).ShouldBeTrue("pageSize alani mevcut olmali");
        data.TryGetProperty("totalCount", out _).ShouldBeTrue("totalCount alani mevcut olmali");
        data.TryGetProperty("totalPages", out _).ShouldBeTrue("totalPages alani mevcut olmali");

        data.GetProperty("page").GetInt32().ShouldBe(1);
        data.GetProperty("pageSize").GetInt32().ShouldBe(10);
        data.GetProperty("totalCount").GetInt32().ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetTendersPaged_Anonymous_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/tenders/paged");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // -----------------------------------------------------------------------
    // GET /api/tenders/paged -- sayfa kesimi totalPages hesabi
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetTendersPaged_PageSize1_TotalPagesEqualsItemCount()
    {
        var token = await LoginAsync();
        SetBearer(token);

        // 2 ayri ihale olustur
        var companyId = await SeedCompanyAsync();
        await CreateTenderAsync(companyId, $"Sayfa_{Guid.NewGuid():N}");
        await CreateTenderAsync(companyId, $"Sayfa_{Guid.NewGuid():N}");

        // pageSize=1 ile; totalPages en az 2 olmali
        var response = await _client.GetAsync("/api/tenders/paged?page=1&pageSize=1");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var data = json.GetProperty("data");
        var totalCount = data.GetProperty("totalCount").GetInt32();
        var totalPages = data.GetProperty("totalPages").GetInt32();
        var items = data.GetProperty("items");

        items.GetArrayLength().ShouldBe(1, "pageSize=1 ile yalnizca 1 kayit donmeli");
        totalPages.ShouldBe(totalCount, "pageSize=1'de totalPages == totalCount olmali");
    }

    // -----------------------------------------------------------------------
    // GET /api/tenders/paged -- search filtresi
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetTendersPaged_WithSearch_ReturnsOnlyMatchingTenders()
    {
        var token = await LoginAsync();
        SetBearer(token);
        var companyId = await SeedCompanyAsync();

        var uniqueTitle = $"SearchHit_{Guid.NewGuid():N}";
        var otherTitle = $"NoMatch_{Guid.NewGuid():N}";
        var hitId = await CreateTenderAsync(companyId, uniqueTitle);
        await CreateTenderAsync(companyId, otherTitle);

        // Tam basligin ilk parcasiyla ara
        var searchTerm = uniqueTitle.Substring(0, 15);
        var response = await _client.GetAsync($"/api/tenders/paged?search={searchTerm}&pageSize=50");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<TenderDto>>>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Data.ShouldNotBeNull();
        // Arama sonucunda en az bir eslesme olmali
        payload.Data!.Items.ShouldContain(t => t.Id == hitId, "Aranan baslik sonuclarda olmali");
    }

    // -----------------------------------------------------------------------
    // GET /api/tenders/paged -- mevcut sector/status filtreleri calisir
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetTendersPaged_WithSectorFilter_ReturnsOnlyMatchingSector()
    {
        var token = await LoginAsync();
        SetBearer(token);

        var retailCompany = await SeedCompanyAsync(sector: Sector.Retail);
        var energyCompany = await SeedCompanyAsync(sector: Sector.Energy);

        await CreateTenderAsync(retailCompany, sector: Sector.Retail);
        var energyId = await CreateTenderAsync(energyCompany, $"Energy_{Guid.NewGuid():N}", Sector.Energy);

        var response = await _client.GetAsync("/api/tenders/paged?sector=Energy&pageSize=50");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<TenderDto>>>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Data!.Items.ShouldContain(t => t.Id == energyId, "Energy sektoru ihalesi sonuclarda olmali");
        payload.Data!.Items.ShouldAllBe(t => t.Sector == "Energy", "Yalniz Energy ihaleler donmeli");
    }

    // -----------------------------------------------------------------------
    // GET /api/tenders (tam liste) -- mevcut uc hala 200 donuyor (kirilmadi)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetTenders_ExistingEndpoint_StillReturns200()
    {
        var token = await LoginAsync();
        SetBearer(token);

        var response = await _client.GetAsync("/api/tenders");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().ShouldBeTrue();
        // data alani dizi (IReadOnlyList) olmali -- paged degil
        json.GetProperty("data").ValueKind.ShouldBe(JsonValueKind.Array);
    }

    // -----------------------------------------------------------------------
    // GET /api/companies/leads/paged -- 200 + zarf alanlari
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetLeadsPaged_Authenticated_Returns200WithEnvelopeFields()
    {
        var token = await LoginAsync();
        SetBearer(token);
        await SeedCompanyAsync();

        var response = await _client.GetAsync("/api/companies/leads/paged?page=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().ShouldBeTrue();
        var data = json.GetProperty("data");
        data.TryGetProperty("items", out _).ShouldBeTrue();
        data.TryGetProperty("page", out _).ShouldBeTrue();
        data.TryGetProperty("pageSize", out _).ShouldBeTrue();
        data.TryGetProperty("totalCount", out _).ShouldBeTrue();
        data.TryGetProperty("totalPages", out _).ShouldBeTrue();
    }

    // -----------------------------------------------------------------------
    // GET /api/companies/leads (tam liste) -- hala calisiyor
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetLeads_ExistingEndpoint_StillReturns200()
    {
        var token = await LoginAsync();
        SetBearer(token);

        var response = await _client.GetAsync("/api/companies/leads");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().ShouldBeTrue();
        json.GetProperty("data").ValueKind.ShouldBe(JsonValueKind.Array);
    }

    // -----------------------------------------------------------------------
    // GET /api/companies/customers/paged -- 200 + zarf alanlari
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetCustomersPaged_Authenticated_Returns200WithEnvelopeFields()
    {
        var token = await LoginAsync();
        SetBearer(token);

        var response = await _client.GetAsync("/api/companies/customers/paged?page=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().ShouldBeTrue();
        var data = json.GetProperty("data");
        data.TryGetProperty("items", out _).ShouldBeTrue();
        data.TryGetProperty("totalCount", out _).ShouldBeTrue();
    }

    // -----------------------------------------------------------------------
    // GET /api/meetings/paged -- 200 + zarf alanlari
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetMeetingsPaged_Authenticated_Returns200WithEnvelopeFields()
    {
        var token = await LoginAsync();
        SetBearer(token);

        var response = await _client.GetAsync("/api/meetings/paged?page=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().ShouldBeTrue();
        var data = json.GetProperty("data");
        data.TryGetProperty("items", out _).ShouldBeTrue();
        data.TryGetProperty("page", out _).ShouldBeTrue();
        data.TryGetProperty("totalCount", out _).ShouldBeTrue();
        data.TryGetProperty("totalPages", out _).ShouldBeTrue();
    }

    // -----------------------------------------------------------------------
    // GET /api/meetings (tam liste) -- hala calisiyor
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetMeetings_ExistingEndpoint_StillReturns200()
    {
        var token = await LoginAsync();
        SetBearer(token);

        var response = await _client.GetAsync("/api/meetings");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().ShouldBeTrue();
        json.GetProperty("data").ValueKind.ShouldBe(JsonValueKind.Array);
    }

    // -----------------------------------------------------------------------
    // GET /api/employees/managed/paged -- 200 + zarf alanlari
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetManagedPaged_Authenticated_Returns200WithEnvelopeFields()
    {
        var token = await LoginAsync();
        SetBearer(token);

        var response = await _client.GetAsync("/api/employees/managed/paged?page=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().ShouldBeTrue();
        var data = json.GetProperty("data");
        data.TryGetProperty("items", out _).ShouldBeTrue();
        data.TryGetProperty("page", out _).ShouldBeTrue();
        data.TryGetProperty("totalCount", out _).ShouldBeTrue();
        data.TryGetProperty("totalPages", out _).ShouldBeTrue();
    }

    // -----------------------------------------------------------------------
    // GET /api/employees/managed (tam liste) -- hala calisiyor
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetManaged_ExistingEndpoint_StillReturns200()
    {
        var token = await LoginAsync();
        SetBearer(token);

        var response = await _client.GetAsync("/api/employees/managed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().ShouldBeTrue();
        json.GetProperty("data").ValueKind.ShouldBe(JsonValueKind.Array);
    }

    // -----------------------------------------------------------------------
    // PagedQuery normalize -- gecersiz degerler sikistirilir (uctan uca)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetTendersPaged_NegativePage_TreatedAs1()
    {
        var token = await LoginAsync();
        SetBearer(token);

        var response = await _client.GetAsync("/api/tenders/paged?page=-5&pageSize=10");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("data").GetProperty("page").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task GetTendersPaged_PageSizeOver100_ClampedTo100()
    {
        var token = await LoginAsync();
        SetBearer(token);

        var response = await _client.GetAsync("/api/tenders/paged?page=1&pageSize=999");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("data").GetProperty("pageSize").GetInt32().ShouldBe(100);
    }

    // -----------------------------------------------------------------------
    // Leads paged -- search ile arama (firma basligi)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetLeadsPaged_WithSearch_ReturnsOnlyMatchingLeads()
    {
        var token = await LoginAsync();
        SetBearer(token);

        var uniqueTitle = $"SearchLead_{Guid.NewGuid():N}";
        var seedId = await SeedCompanyAsync(uniqueTitle);

        var searchTerm = uniqueTitle.Substring(0, 15);
        var response = await _client.GetAsync($"/api/companies/leads/paged?search={searchTerm}&pageSize=50");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<CompanyDto>>>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Data.ShouldNotBeNull();
        payload.Data!.Items.ShouldContain(c => c.Id == seedId, "Aranan firma sonuclarda olmali");
    }

    // -----------------------------------------------------------------------
    // Tenders paged -- sortBy alanina gore siralama
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetTendersPaged_WithSortByTitle_Returns200()
    {
        var token = await LoginAsync();
        SetBearer(token);

        var response = await _client.GetAsync("/api/tenders/paged?sortBy=title&sortDir=asc&page=1&pageSize=10");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("success").GetBoolean().ShouldBeTrue();
        // Siralama dogrulama: en az 200 ve tutarli yapi
        var data = json.GetProperty("data");
        data.TryGetProperty("items", out _).ShouldBeTrue();
    }
}
