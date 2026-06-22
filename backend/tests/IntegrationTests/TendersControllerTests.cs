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
using Oypa.Crm.Contracts.Tenders;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Oypa.Crm.Infrastructure.Identity;
using Oypa.Crm.Infrastructure.Persistence;
using Shouldly;

namespace Oypa.Crm.IntegrationTests;

/// <summary>
/// TendersController uçtan uca testleri: 401, CRUD, sector/status filtresi, 404, PATCH status.
/// </summary>
public sealed class TendersControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string AdminEmail = CustomWebApplicationFactory.AdminEmail;
    private const string AdminPassword = CustomWebApplicationFactory.AdminPassword;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TendersControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.PostConfigure<RateLimiterOptions>(options =>
                {
                    ClearRateLimiterPolicies(options);
                    foreach (var name in new[]
                    {
                        "auth-login", "auth-refresh", "urun-arama", "admin-sensitive"
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
        if (await userManager.FindByEmailAsync(AdminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                FullName = "Test Yöneticisi",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, AdminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -----------------------------------------------------------------------
    // Yardımcı metotlar
    // -----------------------------------------------------------------------

    private async Task<string> LoginAndGetTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(AdminEmail, AdminPassword), JsonOptions);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);
        return payload!.Data!.AccessToken;
    }

    private void SetBearer(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    /// <summary>DB'ye doğrudan firma ekler ve Id'sini döndürür.</summary>
    private async Task<Guid> SeedCompanyAsync(Sector sector = Sector.Retail)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var company = new Company($"Test Firma {Guid.NewGuid():N}", sector, "111", $"{Guid.NewGuid():N}@test.com", "Adres");
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        return company.Id;
    }

    private CreateTenderRequest BuildCreateRequest(Guid companyId, Sector sector = Sector.Retail,
        string? title = null, decimal? estimatedValue = null, decimal? volume = null) =>
        new(companyId, title ?? "Test İhalesi", "IH-001", sector,
            new DateOnly(2026, 12, 1), 10, estimatedValue ?? 1000m, volume ?? 500m, 5, "Açıklama", null);

    // -----------------------------------------------------------------------
    // 401 — Anonim erişim
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetTenders_Anonymous_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/tenders");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTenderById_Anonymous_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/api/tenders/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTender_Anonymous_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var companyId = await SeedCompanyAsync();

        var response = await _client.PostAsJsonAsync("/api/tenders",
            BuildCreateRequest(companyId), JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // -----------------------------------------------------------------------
    // CRUD happy-path
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateTender_ValidRequest_Returns201AndTenderDto()
    {
        var token = await LoginAndGetTokenAsync();
        SetBearer(token);
        var companyId = await SeedCompanyAsync();

        var response = await _client.PostAsJsonAsync("/api/tenders",
            BuildCreateRequest(companyId), JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<TenderDto>>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Success.ShouldBeTrue();
        payload.Data.ShouldNotBeNull();
        payload.Data!.CompanyId.ShouldBe(companyId);
        payload.Data.Title.ShouldBe("Test İhalesi");
        payload.Data.Status.ShouldBe("Hazirlik");
    }

    [Fact]
    public async Task CreateTender_ThenGetAll_AppearsInList()
    {
        var token = await LoginAndGetTokenAsync();
        SetBearer(token);
        var companyId = await SeedCompanyAsync();
        var uniqueTitle = $"Benzersiz İhale {Guid.NewGuid():N}";

        var createResponse = await _client.PostAsJsonAsync("/api/tenders",
            BuildCreateRequest(companyId, title: uniqueTitle), JsonOptions);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<TenderDto>>(JsonOptions))!.Data!;

        var listResponse = await _client.GetAsync("/api/tenders");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await listResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TenderDto>>>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Data.ShouldNotBeNull();
        payload.Data!.Any(t => t.Id == created.Id).ShouldBeTrue(
            "Oluşturulan ihale GET /api/tenders listesinde görünmeli");
    }

    [Fact]
    public async Task GetTenderById_ExistingId_Returns200WithDetails()
    {
        var token = await LoginAndGetTokenAsync();
        SetBearer(token);
        var companyId = await SeedCompanyAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/tenders",
            BuildCreateRequest(companyId), JsonOptions);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<TenderDto>>(JsonOptions))!.Data!;

        var getResponse = await _client.GetAsync($"/api/tenders/{created.Id}");

        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await getResponse.Content.ReadFromJsonAsync<ApiResponse<TenderDto>>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Data!.Id.ShouldBe(created.Id);
    }

    [Fact]
    public async Task UpdateTender_ValidRequest_Returns200WithUpdatedData()
    {
        var token = await LoginAndGetTokenAsync();
        SetBearer(token);
        var companyId = await SeedCompanyAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/tenders",
            BuildCreateRequest(companyId), JsonOptions);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<TenderDto>>(JsonOptions))!.Data!;

        var updateRequest = new UpdateTenderRequest(
            companyId, "Güncel Başlık", "IH-999", Sector.Energy,
            new DateOnly(2026, 12, 20), 50, 5000m, 2500m, 100, "Güncel açıklama", null);

        var updateResponse = await _client.PutAsJsonAsync($"/api/tenders/{created.Id}", updateRequest, JsonOptions);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = (await updateResponse.Content.ReadFromJsonAsync<ApiResponse<TenderDto>>(JsonOptions))!.Data!;
        updated.Title.ShouldBe("Güncel Başlık");
        updated.Sector.ShouldBe("Energy");
        updated.PersonnelCount.ShouldBe(50);
    }

    [Fact]
    public async Task DeleteTender_ExistingId_Returns200AndNotFoundAfterwards()
    {
        var token = await LoginAndGetTokenAsync();
        SetBearer(token);
        var companyId = await SeedCompanyAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/tenders",
            BuildCreateRequest(companyId), JsonOptions);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<TenderDto>>(JsonOptions))!.Data!;

        var deleteResponse = await _client.DeleteAsync($"/api/tenders/{created.Id}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Silindikten sonra GET 404 dönmeli
        var getResponse = await _client.GetAsync($"/api/tenders/{created.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // -----------------------------------------------------------------------
    // PATCH /{id}/status — durum değiştirir
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PatchStatus_ValidStatus_Returns200AndStatusChanged()
    {
        var token = await LoginAndGetTokenAsync();
        SetBearer(token);
        var companyId = await SeedCompanyAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/tenders",
            BuildCreateRequest(companyId), JsonOptions);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<TenderDto>>(JsonOptions))!.Data!;
        created.Status.ShouldBe("Hazirlik");

        var patchResponse = await _client.PatchAsJsonAsync(
            $"/api/tenders/{created.Id}/status",
            new ChangeTenderStatusRequest(TenderStatus.TeklifVerildi),
            JsonOptions);

        patchResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // GET ile durumu doğrula
        var getResponse = await _client.GetAsync($"/api/tenders/{created.Id}");
        var refreshed = (await getResponse.Content.ReadFromJsonAsync<ApiResponse<TenderDto>>(JsonOptions))!.Data!;
        refreshed.Status.ShouldBe("TeklifVerildi");
    }

    [Fact]
    public async Task PatchStatus_UnknownTenderId_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        SetBearer(token);

        var response = await _client.PatchAsJsonAsync(
            $"/api/tenders/{Guid.NewGuid()}/status",
            new ChangeTenderStatusRequest(TenderStatus.Kazanildi),
            JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // -----------------------------------------------------------------------
    // GET /{id} — bilinmeyen id 404
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetTenderById_UnknownId_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        SetBearer(token);

        var response = await _client.GetAsync($"/api/tenders/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // -----------------------------------------------------------------------
    // Filtreler: ?sector= ve ?status=
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetTenders_WithSectorFilter_ReturnsOnlyMatchingSector()
    {
        var token = await LoginAndGetTokenAsync();
        SetBearer(token);

        var retailCompanyId = await SeedCompanyAsync(Sector.Retail);
        var energyCompanyId = await SeedCompanyAsync(Sector.Energy);

        // Retail ihale oluştur
        var retailTitle = $"Retail_{Guid.NewGuid():N}";
        var createRetail = await _client.PostAsJsonAsync("/api/tenders",
            BuildCreateRequest(retailCompanyId, Sector.Retail, retailTitle), JsonOptions);
        createRetail.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Energy ihale oluştur
        var energyTitle = $"Energy_{Guid.NewGuid():N}";
        var createEnergy = await _client.PostAsJsonAsync("/api/tenders",
            BuildCreateRequest(energyCompanyId, Sector.Energy, energyTitle), JsonOptions);
        createEnergy.StatusCode.ShouldBe(HttpStatusCode.Created);
        var energyTender = (await createEnergy.Content.ReadFromJsonAsync<ApiResponse<TenderDto>>(JsonOptions))!.Data!;

        // sector=Energy filtresi ile getir
        var response = await _client.GetAsync("/api/tenders?sector=Energy");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TenderDto>>>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Data.ShouldNotBeNull();

        // Energy ihalesi görünmeli
        payload.Data!.Any(t => t.Id == energyTender.Id).ShouldBeTrue("Energy filtresiyle enerji ihalesi görünmeli");
        // Retail ihalesi bu sonuçta olmamalı
        payload.Data!.All(t => t.Sector == "Energy").ShouldBeTrue(
            "sector=Energy filtresiyle yalnız Energy ihaleleri dönmeli");
    }

    [Fact]
    public async Task GetTenders_WithStatusFilter_ReturnsOnlyMatchingStatus()
    {
        var token = await LoginAndGetTokenAsync();
        SetBearer(token);
        var companyId = await SeedCompanyAsync();

        // Hazirlik ihale oluştur
        var createResponse = await _client.PostAsJsonAsync("/api/tenders",
            BuildCreateRequest(companyId, title: $"Hazirlik_{Guid.NewGuid():N}"), JsonOptions);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var tender = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<TenderDto>>(JsonOptions))!.Data!;

        // TeklifVerildi'ye geçir
        await _client.PatchAsJsonAsync($"/api/tenders/{tender.Id}/status",
            new ChangeTenderStatusRequest(TenderStatus.TeklifVerildi), JsonOptions);

        // status=TeklifVerildi filtresi ile getir
        var response = await _client.GetAsync("/api/tenders?status=TeklifVerildi");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TenderDto>>>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Data.ShouldNotBeNull();
        payload.Data!.Any(t => t.Id == tender.Id).ShouldBeTrue(
            "status=TeklifVerildi filtresiyle durum değiştirilmiş ihale görünmeli");
        payload.Data!.All(t => t.Status == "TeklifVerildi").ShouldBeTrue(
            "status=TeklifVerildi filtresiyle yalnız TeklifVerildi ihaleleri dönmeli");
    }

    [Fact]
    public async Task GetTenders_WithSectorAndStatusFilter_ReturnsOnlyMatchingBoth()
    {
        var token = await LoginAndGetTokenAsync();
        SetBearer(token);

        var tourismCompanyId = await SeedCompanyAsync(Sector.Tourism);
        var energyCompanyId = await SeedCompanyAsync(Sector.Energy);

        // Tourism + Hazirlik
        var createTourism = await _client.PostAsJsonAsync("/api/tenders",
            BuildCreateRequest(tourismCompanyId, Sector.Tourism, $"Tourism_{Guid.NewGuid():N}"), JsonOptions);
        createTourism.StatusCode.ShouldBe(HttpStatusCode.Created);
        var tourismTender = (await createTourism.Content.ReadFromJsonAsync<ApiResponse<TenderDto>>(JsonOptions))!.Data!;

        // Energy + Hazirlik
        var createEnergy = await _client.PostAsJsonAsync("/api/tenders",
            BuildCreateRequest(energyCompanyId, Sector.Energy, $"Energy_{Guid.NewGuid():N}"), JsonOptions);
        createEnergy.StatusCode.ShouldBe(HttpStatusCode.Created);

        // sector=Tourism & status=Hazirlik filtresi
        var response = await _client.GetAsync("/api/tenders?sector=Tourism&status=Hazirlik");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TenderDto>>>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Data.ShouldNotBeNull();
        payload.Data!.Any(t => t.Id == tourismTender.Id).ShouldBeTrue(
            "Tourism+Hazirlik filtresiyle turizm ihalesi görünmeli");
        payload.Data!.All(t => t.Sector == "Tourism" && t.Status == "Hazirlik").ShouldBeTrue(
            "İkili filtreyle yalnız eşleşen ihaleler dönmeli");
    }

    // -----------------------------------------------------------------------
    // Decimal alanlar (EstimatedValue / Volume) sakla-oku
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateTender_WithDecimalFields_StoresAndReturnsPreciseValues()
    {
        var token = await LoginAndGetTokenAsync();
        SetBearer(token);
        var companyId = await SeedCompanyAsync();

        // InMemory provider decimal'i tam olarak saklar; hassasiyet sorunu yok
        var createRequest = BuildCreateRequest(companyId, estimatedValue: 12345.67m, volume: 9876.54m);

        var createResponse = await _client.PostAsJsonAsync("/api/tenders", createRequest, JsonOptions);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<TenderDto>>(JsonOptions))!.Data!;

        var getResponse = await _client.GetAsync($"/api/tenders/{created.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var fetched = (await getResponse.Content.ReadFromJsonAsync<ApiResponse<TenderDto>>(JsonOptions))!.Data!;
        fetched.EstimatedValue.ShouldBe(12345.67m);
        fetched.Volume.ShouldBe(9876.54m);
    }

    // -----------------------------------------------------------------------
    // POST — bilinmeyen CompanyId 404
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateTender_UnknownCompanyId_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        SetBearer(token);

        var response = await _client.PostAsJsonAsync("/api/tenders",
            BuildCreateRequest(Guid.NewGuid()), JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // -----------------------------------------------------------------------
    // GET tüm liste — kimlik doğrulanmış kullanıcı 200 döner
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetTenders_Authenticated_Returns200WithList()
    {
        var token = await LoginAndGetTokenAsync();
        SetBearer(token);

        var response = await _client.GetAsync("/api/tenders");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TenderDto>>>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Success.ShouldBeTrue();
        payload.Data.ShouldNotBeNull();
    }
}
