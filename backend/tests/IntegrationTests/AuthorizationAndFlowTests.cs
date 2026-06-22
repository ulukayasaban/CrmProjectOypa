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
using Oypa.Crm.Contracts.Contacts;
using Oypa.Crm.Contracts.MailDrafts;
using Oypa.Crm.Contracts.Meetings;
using Oypa.Crm.Contracts.SalesReps;
using Oypa.Crm.Domain.Enums;
using Oypa.Crm.Infrastructure.Identity;
using Oypa.Crm.Infrastructure.Persistence;
using Shouldly;

namespace Oypa.Crm.IntegrationTests;

/// <summary>
/// Yetkilendirme (403/404), refresh token rotasyonu/yeniden-kullanım ve
/// contact + meeting + maildraft uçtan uca akışını doğrular.
/// Factory'yi değiştirmeden, ikinci (Sales rollü) kullanıcıyı testte seed eder.
/// </summary>
public sealed class AuthorizationAndFlowTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string SalesEmail = "sales-user@oypa.com.tr";
    private const string SalesPassword = "Sales!23456";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public AuthorizationAndFlowTests(CustomWebApplicationFactory factory)
    {
        // Orijinal factory'yi BOZMADAN, yalnızca bu test sınıfı için yeni bir host
        // katmanı türetip rate limiter politikalarını devre dışı bırakıyoruz.
        // Aksi halde paylaşılan IP partition'ı nedeniyle login limiti (5/dk) testleri
        // 429 ile düşürür. Türetilen host kendi servis sağlayıcısını kurduğundan
        // seed'i bu host'un Services'ı üzerinden yapıyoruz (InitializeAsync).
        _factory = factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                // Önce mevcut (limitleyici) named-policy kayıtlarını temizleyip, ardından
                // aynı isimlerle "limitsiz" politikalar ekliyoruz. Böylece controller'lardaki
                // [EnableRateLimiting("...")] attribute'leri geçerli bir politika bulur ama
                // hiçbir istek reddedilmez. Orijinal factory'ye dokunulmaz.
                services.PostConfigure<RateLimiterOptions>(options =>
                {
                    ClearRateLimiterPolicies(options);
                    foreach (var name in new[]
                    {
                        RateLimitingPolicyNames.AuthLogin,
                        RateLimitingPolicyNames.AuthRefresh,
                        RateLimitingPolicyNames.Search,
                        RateLimitingPolicyNames.AdminSensitive
                    })
                    {
                        options.AddPolicy(name, _ => RateLimitPartition.GetNoLimiter("test"));
                    }
                });
            }));
        _client = _factory.CreateClient();
    }

    /// <summary>
    /// <see cref="RateLimiterOptions"/> üzerindeki kayıtlı named-policy haritalarını
    /// reflection ile temizler (aksi halde aynı isimle yeniden ekleme çakışır).
    /// </summary>
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

    private static class RateLimitingPolicyNames
    {
        public const string AuthLogin = "auth-login";
        public const string AuthRefresh = "auth-refresh";
        public const string Search = "urun-arama";
        public const string AdminSensitive = "admin-sensitive";
    }

    public async Task InitializeAsync()
    {
        // Türetilen host kendi izole InMemory store'unu kurduğundan, seed'i bu host'un
        // servis sağlayıcısı üzerinden yapıyoruz: rol + admin (CustomWebApplicationFactory
        // ile aynı kimlik bilgileri) + Sales rollü ikinci kullanıcı.
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

    private async Task<AuthResponse> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);
        return payload!.Data!;
    }

    private void SetBearer(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // ---- 403: Sales rollü (admin olmayan) kullanıcı admin uçlarına erişemez ----

    [Fact]
    public async Task CreateSalesRep_AsNonAdmin_Returns403()
    {
        var auth = await LoginAsync(SalesEmail, SalesPassword);
        SetBearer(auth.AccessToken);

        var response = await _client.PostAsJsonAsync("/api/salesreps",
            new CreateSalesRepRequest("Yeni Temsilci", "rep@oypa.com"), JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Register_AsNonAdmin_Returns403()
    {
        var auth = await LoginAsync(SalesEmail, SalesPassword);
        SetBearer(auth.AccessToken);

        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterUserRequest("new@oypa.com", "Parola12", "Yeni Kullanıcı", "Sales"), JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ---- 404: bilinmeyen firma ----

    [Fact]
    public async Task GetCompany_UnknownId_Returns404()
    {
        var auth = await LoginAsync(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        SetBearer(auth.AccessToken);

        var response = await _client.GetAsync($"/api/companies/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ---- Refresh rotasyonu + yeniden kullanım tespiti ----

    [Fact]
    public async Task Refresh_ValidToken_IssuesNewToken_AndReuseOfOldReturns401()
    {
        var auth = await LoginAsync(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        var originalRefresh = auth.RefreshToken;

        // İlk refresh: yeni token üretir, eskisini iptal eder.
        var firstRefresh = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest(originalRefresh), JsonOptions);
        firstRefresh.StatusCode.ShouldBe(HttpStatusCode.OK);
        var refreshed = await firstRefresh.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);
        refreshed!.Data!.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        refreshed.Data.RefreshToken.ShouldNotBe(originalRefresh);

        // Eski (iptal edilmiş) refresh token tekrar kullanılırsa 401.
        var reuse = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest(originalRefresh), JsonOptions);
        reuse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ---- Contact + meeting happy path -> maildraft ----

    [Fact]
    public async Task ScheduleMeeting_HappyPath_CreatesMeetingAndMailDraft()
    {
        var auth = await LoginAsync(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        SetBearer(auth.AccessToken);

        // 1) Lead oluştur.
        var createCompany = await _client.PostAsJsonAsync("/api/companies",
            new CreateCompanyRequest("Akış A.Ş.", Sector.Energy, "0212", "akis@a.com", "Adres"), JsonOptions);
        createCompany.StatusCode.ShouldBe(HttpStatusCode.Created);
        var company = (await createCompany.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>(JsonOptions))!.Data!;

        // 2) İlgili kişi ekle.
        var createContact = await _client.PostAsJsonAsync($"/api/companies/{company.Id}/contacts",
            new CreateContactRequest("İlgili Kişi", "kisi@a.com", "555"), JsonOptions);
        createContact.StatusCode.ShouldBe(HttpStatusCode.Created);
        var contact = (await createContact.Content.ReadFromJsonAsync<ApiResponse<ContactDto>>(JsonOptions))!.Data!;

        // 3) Satış temsilcisi oluştur (admin).
        var createRep = await _client.PostAsJsonAsync("/api/salesreps",
            new CreateSalesRepRequest("Temsilci", "temsilci@oypa.com"), JsonOptions);
        createRep.StatusCode.ShouldBe(HttpStatusCode.Created);
        var rep = (await createRep.Content.ReadFromJsonAsync<ApiResponse<SalesRepDto>>(JsonOptions))!.Data!;

        // 4) Görüşme planla -> 201.
        var schedule = await _client.PostAsJsonAsync("/api/meetings",
            new ScheduleMeetingRequest(
                company.Id, contact.Id, rep.Id,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                new TimeOnly(14, 30), "Görüşme Adresi", MeetingMethod.Visit),
            JsonOptions);
        schedule.StatusCode.ShouldBe(HttpStatusCode.Created);
        var meeting = (await schedule.Content.ReadFromJsonAsync<ApiResponse<MeetingDto>>(JsonOptions))!.Data!;

        // 5) Mail taslakları içinde bu görüşmeye ait taslak görünmeli.
        var draftsResponse = await _client.GetAsync("/api/maildrafts");
        draftsResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var drafts = await draftsResponse.Content
            .ReadFromJsonAsync<ApiResponse<IReadOnlyList<MailDraftDto>>>(JsonOptions);
        drafts!.Data!.ShouldContain(d => d.MeetingId == meeting.Id && d.To == rep.Email && !d.Sent);
    }

    // ---- .eml endpoint ----

    [Fact]
    public async Task GetEml_AfterScheduleMeeting_Returns200WithRfc822Content()
    {
        var auth = await LoginAsync(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        SetBearer(auth.AccessToken);

        // Firma, ilgili kişi ve temsilci oluştur.
        var createCompany = await _client.PostAsJsonAsync("/api/companies",
            new CreateCompanyRequest("Eml Test A.Ş.", Sector.Energy, "0212", "emltest@a.com", "Adres"), JsonOptions);
        createCompany.StatusCode.ShouldBe(HttpStatusCode.Created);
        var company = (await createCompany.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>(JsonOptions))!.Data!;

        var createContact = await _client.PostAsJsonAsync($"/api/companies/{company.Id}/contacts",
            new CreateContactRequest("Eml Kişi", "emlkisi@a.com", "111"), JsonOptions);
        createContact.StatusCode.ShouldBe(HttpStatusCode.Created);
        var contact = (await createContact.Content.ReadFromJsonAsync<ApiResponse<ContactDto>>(JsonOptions))!.Data!;

        var createRep = await _client.PostAsJsonAsync("/api/salesreps",
            new CreateSalesRepRequest("Eml Temsilci", "emltemsilci@oypa.com"), JsonOptions);
        createRep.StatusCode.ShouldBe(HttpStatusCode.Created);
        var rep = (await createRep.Content.ReadFromJsonAsync<ApiResponse<SalesRepDto>>(JsonOptions))!.Data!;

        var schedule = await _client.PostAsJsonAsync("/api/meetings",
            new ScheduleMeetingRequest(
                company.Id, contact.Id, rep.Id,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
                new TimeOnly(10, 0), "Eml Adresi", MeetingMethod.Visit),
            JsonOptions);
        schedule.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Taslak id'sini bul.
        var draftsResponse = await _client.GetAsync("/api/maildrafts");
        draftsResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var drafts = await draftsResponse.Content
            .ReadFromJsonAsync<ApiResponse<IReadOnlyList<MailDraftDto>>>(JsonOptions);
        var draft = drafts!.Data!.First(d => d.To == rep.Email);

        // .eml dosyasını indir.
        var emlResponse = await _client.GetAsync($"/api/maildrafts/{draft.Id}/eml");
        emlResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        emlResponse.Content.Headers.ContentType!.MediaType.ShouldBe("message/rfc822");

        var emlBytes = await emlResponse.Content.ReadAsByteArrayAsync();
        var emlText = System.Text.Encoding.UTF8.GetString(emlBytes);
        emlText.ShouldContain("X-Unsent: 1");
        emlText.ShouldContain("To: ");
    }

    // ---- by-meeting endpoint ----

    [Fact]
    public async Task GetByMeeting_ExistingMeeting_ReturnsMailDraftDto()
    {
        var auth = await LoginAsync(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        SetBearer(auth.AccessToken);

        var createCompany = await _client.PostAsJsonAsync("/api/companies",
            new CreateCompanyRequest("ByMeeting A.Ş.", Sector.Retail, "0212", "bymeeting@a.com", "Adres"), JsonOptions);
        createCompany.StatusCode.ShouldBe(HttpStatusCode.Created);
        var company = (await createCompany.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>(JsonOptions))!.Data!;

        var createContact = await _client.PostAsJsonAsync($"/api/companies/{company.Id}/contacts",
            new CreateContactRequest("BM Kişi", "bmkisi@a.com", "222"), JsonOptions);
        createContact.StatusCode.ShouldBe(HttpStatusCode.Created);
        var contact = (await createContact.Content.ReadFromJsonAsync<ApiResponse<ContactDto>>(JsonOptions))!.Data!;

        var createRep = await _client.PostAsJsonAsync("/api/salesreps",
            new CreateSalesRepRequest("BM Temsilci", "bmtemsilci@oypa.com"), JsonOptions);
        createRep.StatusCode.ShouldBe(HttpStatusCode.Created);
        var rep = (await createRep.Content.ReadFromJsonAsync<ApiResponse<SalesRepDto>>(JsonOptions))!.Data!;

        var schedule = await _client.PostAsJsonAsync("/api/meetings",
            new ScheduleMeetingRequest(
                company.Id, contact.Id, rep.Id,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(4)),
                new TimeOnly(11, 0), "BM Adresi", MeetingMethod.Phone),
            JsonOptions);
        schedule.StatusCode.ShouldBe(HttpStatusCode.Created);
        var meeting = (await schedule.Content.ReadFromJsonAsync<ApiResponse<MeetingDto>>(JsonOptions))!.Data!;

        // by-meeting endpoint -> 200 + MailDraftDto (To = rep email, Cc = contact email).
        var byMeetingResponse = await _client.GetAsync($"/api/maildrafts/by-meeting/{meeting.Id}");
        byMeetingResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await byMeetingResponse.Content.ReadFromJsonAsync<ApiResponse<MailDraftDto>>(JsonOptions);
        result!.Success.ShouldBeTrue();
        result.Data!.To.ShouldBe(rep.Email);
        result.Data.Cc.ShouldBe(contact.Email);
    }

    [Fact]
    public async Task GetByMeeting_UnknownMeetingId_Returns404()
    {
        var auth = await LoginAsync(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        SetBearer(auth.AccessToken);

        var response = await _client.GetAsync($"/api/maildrafts/by-meeting/{Guid.NewGuid()}");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
