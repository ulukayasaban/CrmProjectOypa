using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Oypa.Crm.Contracts.Auth;
using Oypa.Crm.Contracts.Common;
using Oypa.Crm.Contracts.Companies;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Oypa.Crm.Infrastructure.Identity;
using Oypa.Crm.Infrastructure.Persistence;
using Shouldly;

namespace Oypa.Crm.IntegrationTests;

/// <summary>
/// GET /api/companies/{companyId}/notes ve POST /api/companies/{companyId}/notes endpoint testleri.
/// </summary>
public sealed class CompanyNotesControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string AdminEmail = CustomWebApplicationFactory.AdminEmail;
    private const string AdminPassword = CustomWebApplicationFactory.AdminPassword;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CompanyNotesControllerTests(CustomWebApplicationFactory factory)
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

    /// <summary>DB'ye doğrudan firma ekler.</summary>
    private async Task<Guid> SeedCompanyAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var company = new Company("Test Not Firması", Sector.Retail, "5550000000", "firma@test.com", "Test Adres");
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        return company.Id;
    }

    // -----------------------------------------------------------------------
    // GET /api/companies/{companyId}/notes — yetkilendirme
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetNotes_Anonymous_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/api/companies/{Guid.NewGuid()}/notes");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // -----------------------------------------------------------------------
    // POST /api/companies/{companyId}/notes — yetkilendirme
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PostNote_Anonymous_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync(
            $"/api/companies/{Guid.NewGuid()}/notes",
            new CreateCompanyNoteRequest("İçerik"), JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // -----------------------------------------------------------------------
    // POST /api/companies/{companyId}/notes — bilinmeyen firma
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PostNote_UnknownCompanyId_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        SetBearer(token);

        var response = await _client.PostAsJsonAsync(
            $"/api/companies/{Guid.NewGuid()}/notes",
            new CreateCompanyNoteRequest("İçerik"), JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // -----------------------------------------------------------------------
    // POST + GET — not ekle, kronolojik sıra ve AuthorName dolu
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PostNote_ValidCompany_Returns201AndNoteAppearsInGetNotes()
    {
        var companyId = await SeedCompanyAsync();

        var token = await LoginAndGetTokenAsync();
        SetBearer(token);

        // Not ekle
        var postResponse = await _client.PostAsJsonAsync(
            $"/api/companies/{companyId}/notes",
            new CreateCompanyNoteRequest("Firma hakkında önemli not."), JsonOptions);

        postResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var postPayload = await postResponse.Content.ReadFromJsonAsync<ApiResponse<CompanyNoteDto>>(JsonOptions);
        postPayload.ShouldNotBeNull();
        postPayload!.Success.ShouldBeTrue();
        postPayload.Data.ShouldNotBeNull();
        postPayload.Data!.Content.ShouldBe("Firma hakkında önemli not.");

        // GET ile listele
        var getResponse = await _client.GetAsync($"/api/companies/{companyId}/notes");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var getPayload = await getResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CompanyNoteDto>>>(JsonOptions);
        getPayload.ShouldNotBeNull();
        getPayload!.Data.ShouldNotBeNull();
        getPayload.Data!.ShouldNotBeEmpty();
        getPayload.Data.Any(n => n.Content == "Firma hakkında önemli not.").ShouldBeTrue();
    }

    [Fact]
    public async Task PostNote_AuthorNameIsPopulatedFromAuthenticatedUser()
    {
        var companyId = await SeedCompanyAsync();

        var token = await LoginAndGetTokenAsync();
        SetBearer(token);

        var response = await _client.PostAsJsonAsync(
            $"/api/companies/{companyId}/notes",
            new CreateCompanyNoteRequest("Yazar bilgisi dolu olmalı"), JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<CompanyNoteDto>>(JsonOptions);
        var note = payload!.Data!;
        note.AuthorName.ShouldNotBeNullOrWhiteSpace("Yazar adı boş olmamalı");
    }

    [Fact]
    public async Task GetNotes_MultipleNotes_ReturnedNewestFirst()
    {
        var companyId = await SeedCompanyAsync();

        var token = await LoginAndGetTokenAsync();
        SetBearer(token);

        // İki not ekle
        await _client.PostAsJsonAsync(
            $"/api/companies/{companyId}/notes",
            new CreateCompanyNoteRequest("Birinci not"), JsonOptions);

        await _client.PostAsJsonAsync(
            $"/api/companies/{companyId}/notes",
            new CreateCompanyNoteRequest("İkinci not"), JsonOptions);

        // GET
        var getResponse = await _client.GetAsync($"/api/companies/{companyId}/notes");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var getPayload = await getResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CompanyNoteDto>>>(JsonOptions);
        var notes = getPayload!.Data!.ToList();

        notes.Count.ShouldBeGreaterThanOrEqualTo(2);

        // Yeni→eski: ilk not daha yeni olmalı (CreatedAtUtc azalan)
        for (var i = 0; i < notes.Count - 1; i++)
        {
            notes[i].CreatedAtUtc.ShouldBeGreaterThanOrEqualTo(notes[i + 1].CreatedAtUtc);
        }
    }

    [Fact]
    public async Task PostNote_EmptyContent_ReturnsBadRequest()
    {
        var companyId = await SeedCompanyAsync();

        var token = await LoginAndGetTokenAsync();
        SetBearer(token);

        var response = await _client.PostAsJsonAsync(
            $"/api/companies/{companyId}/notes",
            new CreateCompanyNoteRequest(string.Empty), JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
