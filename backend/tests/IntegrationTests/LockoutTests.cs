using System.Net;
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
using Oypa.Crm.Infrastructure.Identity;
using Oypa.Crm.Infrastructure.Persistence;
using Shouldly;

namespace Oypa.Crm.IntegrationTests;

/// <summary>
/// Hesap kilitleme (lockout) davranışını doğrular: 5 ardışık başarısız giriş hesabı
/// kilitler; doğru parola ile bile giriş yapılamaz. Rate limiter devre dışı bırakılır,
/// aksi hâlde 5/dk IP limiti lockout'a ulaşmayı engeller.
/// </summary>
public sealed class LockoutTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string LockoutUserEmail = "lockout-victim@oypa.com.tr";
    private const string LockoutUserPassword = "Lockout!23456";
    private const string WrongPassword = "WrongPassword!99";

    private const string OtherUserEmail = "lockout-other@oypa.com.tr";
    private const string OtherUserPassword = "OtherUser!23456";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public LockoutTests(CustomWebApplicationFactory factory)
    {
        // AuthorizationAndFlowTests ile aynı yaklaşım: rate limiter politikalarını
        // devre dışı bırakarak türetilmiş bir host oluşturuyoruz. Böylece 5 ardışık
        // başarısız giriş isteği IP rate-limit'ine (5/dk) takılmadan lockout'a ulaşır.
        _factory = factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
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

    // <see cref="RateLimiterOptions"/> üzerindeki named-policy haritalarını reflection
    // ile temizler; aksi hâlde aynı isimle ekleme InvalidOperationException fırlatır.
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
        // Türetilmiş host kendi izole InMemory store'unu kullandığından, seed'i bu
        // host'un servis sağlayıcısı üzerinden yapıyoruz.
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

        // Lockout testinin hedef kullanıcısı.
        await EnsureUserAsync(userManager, LockoutUserEmail, LockoutUserPassword, "Kilit Kurbanı", "Sales");

        // Lockout'tan etkilenmemesi gereken bağımsız kullanıcı.
        await EnsureUserAsync(userManager, OtherUserEmail, OtherUserPassword, "Bağımsız Kullanıcı", "Sales");

        // Admin kullanıcı (varsa mevcut kaydı korur).
        await EnsureUserAsync(userManager,
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword,
            "Test Yöneticisi", "Admin");
    }

    public Task DisposeAsync() => Task.CompletedTask;

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

    // ---- 5 başarısız giriş → hesap kilitlenir, doğru parola ile de 401 döner ----

    [Fact]
    public async Task Login_AfterFiveFailedAttempts_AccountIsLockedAndReturns401()
    {
        // Arrange: geçerli e-posta + yanlış parola ile 5 kez giriş denemesi.
        for (var i = 0; i < 5; i++)
        {
            var failResponse = await _client.PostAsJsonAsync("/api/auth/login",
                new LoginRequest(LockoutUserEmail, WrongPassword));

            // İlk 4 deneme null döner → AuthService 401 fırlatır; 5. deneme lockout'u tetikler.
            failResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        // Act: doğru parola ile giriş — hesap kilitli olduğundan yine 401 bekliyoruz.
        var lockedResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(LockoutUserEmail, LockoutUserPassword));

        // Assert: hesap kilitli → 401
        lockedResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var body = await lockedResponse.Content
            .ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);
        body.ShouldNotBeNull();
        body!.Success.ShouldBeFalse();
        body.Message.ShouldContain("kilitlendi");
    }

    // ---- (Opsiyonel) Başka bir kullanıcı lockout'tan etkilenmemeli ----

    [Fact]
    public async Task Login_AfterFiveFailedAttemptsOnVictim_OtherUserIsUnaffected()
    {
        // Arrange: sadece LockoutUserEmail hesabını kilitle.
        for (var i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync("/api/auth/login",
                new LoginRequest(LockoutUserEmail, WrongPassword));
        }

        // Act: bağımsız kullanıcı doğru parola ile giriş yapabilmeli.
        var otherResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(OtherUserEmail, OtherUserPassword));

        // Assert: bağımsız kullanıcı etkilenmedi → 200 + token
        otherResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await otherResponse.Content
            .ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Success.ShouldBeTrue();
        payload.Data!.AccessToken.ShouldNotBeNullOrWhiteSpace();
    }
}
