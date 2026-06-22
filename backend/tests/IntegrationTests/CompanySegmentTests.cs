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
using Oypa.Crm.Domain.Enums;
using Oypa.Crm.Infrastructure.Identity;
using Oypa.Crm.Infrastructure.Persistence;
using Shouldly;

namespace Oypa.Crm.IntegrationTests;

/// <summary>
/// Lead/müşteri segment filtreleme ve müşteri-durum güncelleme uçtan uca testleri.
/// </summary>
public sealed class CompanySegmentTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public CompanySegmentTests(CustomWebApplicationFactory factory)
    {
        // Rate limiter'ı devre dışı bırakarak türetilmiş bir host kullanıyoruz.
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

    /// <summary>Türetilmiş host üzerinde rol ve admin kullanıcısını seed eder.</summary>
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
        if (await userManager.FindByEmailAsync(CustomWebApplicationFactory.AdminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = CustomWebApplicationFactory.AdminEmail,
                Email = CustomWebApplicationFactory.AdminEmail,
                FullName = "Test Yöneticisi",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, CustomWebApplicationFactory.AdminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<string> LoginAndGetTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword),
            JsonOptions);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);
        return payload!.Data!.AccessToken;
    }

    // ---- Lead segment filtre ----

    [Fact]
    public async Task GetLeads_WithNewStatusFilter_ContainsNewLead_AndNotLostLead()
    {
        var token = await LoginAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Lead oluştur (varsayılan LeadStatus = New).
        var createResp = await _client.PostAsJsonAsync("/api/companies",
            new CreateCompanyRequest("Segment Lead A.Ş.", Sector.Retail, "0212", "seg@a.com", "Adres"),
            JsonOptions);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var company = (await createResp.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>(JsonOptions))!.Data!;

        // GET /companies/leads?status=New — lead görünmeli.
        var newLeadsResp = await _client.GetAsync("/api/companies/leads?status=New");
        newLeadsResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var newLeads = (await newLeadsResp.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CompanyDto>>>(JsonOptions))!.Data!;
        newLeads.ShouldContain(c => c.Id == company.Id);

        // GET /companies/leads?status=Lost — aynı lead görünmemeli.
        var lostLeadsResp = await _client.GetAsync("/api/companies/leads?status=Lost");
        lostLeadsResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var lostLeads = (await lostLeadsResp.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CompanyDto>>>(JsonOptions))!.Data!;
        lostLeads.ShouldNotContain(c => c.Id == company.Id);
    }

    // ---- Müşteri segment güncelleme ve filtre ----

    [Fact]
    public async Task ConvertToCustomer_ThenSetPassiveStatus_AppearsInPassiveNotActive()
    {
        var token = await LoginAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1) Lead oluştur.
        var createResp = await _client.PostAsJsonAsync("/api/companies",
            new CreateCompanyRequest("Müşteri Segment A.Ş.", Sector.Energy, "0312", "musteri@a.com", "Adres"),
            JsonOptions);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var company = (await createResp.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>(JsonOptions))!.Data!;

        // 2) Müşteriye dönüştür.
        var convertResp = await _client.PostAsync($"/api/companies/{company.Id}/convert", null);
        convertResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        // 3) PATCH /companies/{id}/customer-status {status: "Passive"} → 200.
        var patchResp = await _client.PatchAsJsonAsync(
            $"/api/companies/{company.Id}/customer-status",
            new SetCustomerStatusRequest(CustomerStatus.Passive),
            JsonOptions);
        patchResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        // 4) GET /companies/customers?status=Passive — görünmeli.
        var passiveResp = await _client.GetAsync("/api/companies/customers?status=Passive");
        passiveResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var passiveList = (await passiveResp.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CompanyDto>>>(JsonOptions))!.Data!;
        passiveList.ShouldContain(c => c.Id == company.Id);

        // 5) GET /companies/customers?status=Active — görünmemeli.
        var activeResp = await _client.GetAsync("/api/companies/customers?status=Active");
        activeResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var activeList = (await activeResp.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CompanyDto>>>(JsonOptions))!.Data!;
        activeList.ShouldNotContain(c => c.Id == company.Id);
    }

    // ---- Lead firmaya customer-status → 409 ----

    [Fact]
    public async Task SetCustomerStatus_OnLeadCompany_Returns409()
    {
        var token = await LoginAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Lead oluştur (dönüştürme yapılmıyor).
        var createResp = await _client.PostAsJsonAsync("/api/companies",
            new CreateCompanyRequest("Çakışma Lead A.Ş.", Sector.Other, "0412", "conflict@a.com", "Adres"),
            JsonOptions);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var company = (await createResp.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>(JsonOptions))!.Data!;

        // PATCH customer-status lead firmada 409 döndürmeli.
        var patchResp = await _client.PatchAsJsonAsync(
            $"/api/companies/{company.Id}/customer-status",
            new SetCustomerStatusRequest(CustomerStatus.Passive),
            JsonOptions);
        patchResp.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }
}
