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
using Oypa.Crm.Contracts.SalesReps;
using Oypa.Crm.Domain.Enums;
using Oypa.Crm.Infrastructure.Identity;
using Oypa.Crm.Infrastructure.Persistence;
using Shouldly;

namespace Oypa.Crm.IntegrationTests;

/// <summary>
/// PATCH /api/companies/{id}/assign-rep uçtan uca testleri:
/// başarılı atama, havuza alma (null) ve yetkilendirme reddi.
/// </summary>
public sealed class CompanyAssignmentTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string SalesEmail = "sales-assign@oypa.com.tr";
    private const string SalesPassword = "Sales!23456";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public CompanyAssignmentTests(CustomWebApplicationFactory factory)
    {
        // Rate limiter'ı devre dışı bırakarak türetilmiş bir host kullanıyoruz;
        // böylece paylaşılan login limitinden (5/dk) etkilenmiyoruz.
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
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword, "Test Yöneticisi", "Admin");
        await EnsureUserAsync(userManager,
            SalesEmail, SalesPassword, "Satış Kullanıcısı", "Sales");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager, string email, string password, string fullName, string role)
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
            new LoginRequest(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword),
            JsonOptions);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);
        return payload!.Data!.AccessToken;
    }

    private async Task<string> LoginSalesAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(SalesEmail, SalesPassword),
            JsonOptions);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);
        return payload!.Data!.AccessToken;
    }

    private void SetBearer(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // ---- Admin: temsilci atama ve GET ile doğrulama ----

    [Fact]
    public async Task AssignSalesRep_AsAdmin_Returns200_AndRepNameAppearsInLeadsList()
    {
        var token = await LoginAdminAsync();
        SetBearer(token);

        // Satış temsilcisi oluştur.
        var createRepResp = await _client.PostAsJsonAsync("/api/salesreps",
            new CreateSalesRepRequest("Atama Temsilcisi", "atama-rep@oypa.com"), JsonOptions);
        createRepResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var rep = (await createRepResp.Content.ReadFromJsonAsync<ApiResponse<SalesRepDto>>(JsonOptions))!.Data!;

        // Lead firma oluştur.
        var createCompanyResp = await _client.PostAsJsonAsync("/api/companies",
            new CreateCompanyRequest("Atama Test A.Ş.", Sector.Retail, "0212111", "atama@test.com", "Adres"),
            JsonOptions);
        createCompanyResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var company = (await createCompanyResp.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>(JsonOptions))!.Data!;

        // Temsilci ata.
        var assignResp = await _client.PatchAsJsonAsync(
            $"/api/companies/{company.Id}/assign-rep",
            new AssignSalesRepRequest(rep.Id),
            JsonOptions);
        assignResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        // GET /companies/leads listesinde firmanın assignedSalesRepName doğru olmalı.
        var leadsResp = await _client.GetAsync("/api/companies/leads");
        leadsResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var leads = (await leadsResp.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CompanyDto>>>(JsonOptions))!.Data!;
        var assigned = leads.SingleOrDefault(c => c.Id == company.Id);
        assigned.ShouldNotBeNull();
        assigned!.AssignedSalesRepId.ShouldBe(rep.Id);
        assigned.AssignedSalesRepName.ShouldBe("Atama Temsilcisi");
    }

    // ---- Admin: null ile havuza alma ----

    [Fact]
    public async Task AssignSalesRep_WithNullRepId_Returns200_AndRepNameBecomesNull()
    {
        var token = await LoginAdminAsync();
        SetBearer(token);

        // Satış temsilcisi oluştur.
        var createRepResp = await _client.PostAsJsonAsync("/api/salesreps",
            new CreateSalesRepRequest("Havuz Temsilcisi", "havuz-rep@oypa.com"), JsonOptions);
        createRepResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var rep = (await createRepResp.Content.ReadFromJsonAsync<ApiResponse<SalesRepDto>>(JsonOptions))!.Data!;

        // Lead firma oluştur.
        var createCompanyResp = await _client.PostAsJsonAsync("/api/companies",
            new CreateCompanyRequest("Havuz Test A.Ş.", Sector.Energy, "0312111", "havuz@test.com", "Adres"),
            JsonOptions);
        createCompanyResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var company = (await createCompanyResp.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>(JsonOptions))!.Data!;

        // Önce temsilci ata.
        var assignResp = await _client.PatchAsJsonAsync(
            $"/api/companies/{company.Id}/assign-rep",
            new AssignSalesRepRequest(rep.Id),
            JsonOptions);
        assignResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Sonra null ile havuza al.
        var unassignResp = await _client.PatchAsJsonAsync(
            $"/api/companies/{company.Id}/assign-rep",
            new AssignSalesRepRequest(null),
            JsonOptions);
        unassignResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        // GET ile doğrula: assignedSalesRepName null olmalı.
        var getResp = await _client.GetAsync($"/api/companies/{company.Id}");
        getResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = (await getResp.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>(JsonOptions))!.Data!;
        result.AssignedSalesRepId.ShouldBeNull();
        result.AssignedSalesRepName.ShouldBeNull();
    }

    // ---- Non-admin (Sales rolü) → 403 ----

    [Fact]
    public async Task AssignSalesRep_AsNonAdmin_Returns403()
    {
        // Önce admin ile lead ve rep oluştur.
        var adminToken = await LoginAdminAsync();
        SetBearer(adminToken);

        var createCompanyResp = await _client.PostAsJsonAsync("/api/companies",
            new CreateCompanyRequest("Yetkisiz Test A.Ş.", Sector.Other, "0412111", "yetkisiz@test.com", "Adres"),
            JsonOptions);
        createCompanyResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var company = (await createCompanyResp.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>(JsonOptions))!.Data!;

        // Sales rolüyle PATCH → 403 bekleniyor.
        var salesToken = await LoginSalesAsync();
        SetBearer(salesToken);

        var assignResp = await _client.PatchAsJsonAsync(
            $"/api/companies/{company.Id}/assign-rep",
            new AssignSalesRepRequest(Guid.NewGuid()),
            JsonOptions);
        assignResp.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ---- (Opsiyonel) Müşteriye dönüştürülen firmada atama korunuyor ----

    [Fact]
    public async Task AssignSalesRep_ThenConvertToCustomer_AssignmentIsPreserved()
    {
        var token = await LoginAdminAsync();
        SetBearer(token);

        // Satış temsilcisi oluştur.
        var createRepResp = await _client.PostAsJsonAsync("/api/salesreps",
            new CreateSalesRepRequest("Dönüşüm Temsilcisi", "donusum-rep@oypa.com"), JsonOptions);
        createRepResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var rep = (await createRepResp.Content.ReadFromJsonAsync<ApiResponse<SalesRepDto>>(JsonOptions))!.Data!;

        // Lead oluştur.
        var createCompanyResp = await _client.PostAsJsonAsync("/api/companies",
            new CreateCompanyRequest("Dönüşüm Test A.Ş.", Sector.FacilityManagement, "0512111", "donusum@test.com", "Adres"),
            JsonOptions);
        createCompanyResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var company = (await createCompanyResp.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>(JsonOptions))!.Data!;

        // Temsilci ata.
        var assignResp = await _client.PatchAsJsonAsync(
            $"/api/companies/{company.Id}/assign-rep",
            new AssignSalesRepRequest(rep.Id),
            JsonOptions);
        assignResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Müşteriye dönüştür.
        var convertResp = await _client.PostAsync($"/api/companies/{company.Id}/convert", null);
        convertResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        // GET ile atama bilgisi korunmalı.
        var getResp = await _client.GetAsync($"/api/companies/{company.Id}");
        getResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = (await getResp.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>(JsonOptions))!.Data!;
        result.AssignedSalesRepId.ShouldBe(rep.Id);
        result.AssignedSalesRepName.ShouldBe("Dönüşüm Temsilcisi");
        result.Type.ShouldBe(CompanyType.Customer);
    }
}
