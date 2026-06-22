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
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Contracts.Auth;
using Oypa.Crm.Contracts.Common;
using Oypa.Crm.Contracts.Notifications;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Oypa.Crm.Infrastructure.Identity;
using Oypa.Crm.Infrastructure.Persistence;
using NSubstitute;
using Shouldly;

namespace Oypa.Crm.IntegrationTests;

/// <summary>
/// NotificationsController uçtan uca integration testleri.
/// Per-alıcı izolasyon, yetki, mark-read ve SendToUnit senaryolarını kapsar.
/// </summary>
public sealed class NotificationsControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    // Org hiyerarşisi: Umur (kök, Admin) → Avniye (Admin, yönetici) → Muhammed (Sales, yaprak)
    private const string UmurEmail = "notif.umur@oypa.com.tr";
    private const string AvniyeEmail = "notif.avniye@oypa.com.tr";
    private const string MuhammedEmail = "notif.muhammed@oypa.com.tr";
    private const string OrgPassword = "Oypa!2026";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public NotificationsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                // Rate limiter'ı test ortamında devre dışı bırak
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

                // SignalRRealtimeNotifier yerine no-op fake kullan (SignalR hub bağlantısı olmadan test)
                var existing = services.FirstOrDefault(d => d.ServiceType == typeof(IRealtimeNotifier));
                if (existing is not null)
                    services.Remove(existing);
                services.AddScoped<IRealtimeNotifier>(_ => new NoOpRealtimeNotifier());
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

        await SeedOrgHierarchyAsync(db, userManager);
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

    private static async Task SeedOrgHierarchyAsync(
        AppDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        // Daha önce seed edilmişse atla
        if (await db.Employees.AnyAsync(e => e.Email == UmurEmail))
            return;

        var umur = new Employee("Satış Direktörü", "Notif Umur", UmurEmail);
        await LinkOrgAccountAsync(umur, UmurEmail, "Notif Umur", OrgPassword, "Admin", userManager);
        db.Employees.Add(umur);
        await db.SaveChangesAsync();

        var avniye = new Employee("Satış Müdürü", "Notif Avniye", AvniyeEmail, umur.Id);
        await LinkOrgAccountAsync(avniye, AvniyeEmail, "Notif Avniye", OrgPassword, "Admin", userManager);
        db.Employees.Add(avniye);
        await db.SaveChangesAsync();

        var muhammed = new Employee("Satış Uzmanı", "Notif Muhammed", MuhammedEmail, avniye.Id);
        await LinkOrgAccountAsync(muhammed, MuhammedEmail, "Notif Muhammed", OrgPassword, "Sales", userManager);
        db.Employees.Add(muhammed);
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
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return;

        await userManager.AddToRoleAsync(user, role);
        employee.LinkAccount(user.Id);
    }

    private async Task<AuthResponse> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, password), JsonOptions);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"{email} ile giriş başarısız");
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);
        return payload!.Data!;
    }

    private async Task<Guid> GetUserIdAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        return user!.Id;
    }

    private async Task<Guid> GetEmployeeIdAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emp = await db.Employees.SingleAsync(e => e.Email == email);
        return emp.Id;
    }

    /// <summary>
    /// Doğrudan DB'ye bildirim ekler; test senaryolarında önceden var olan bildirim oluşturmak için.
    /// </summary>
    private async Task<Guid> SeedNotificationAsync(Guid recipientUserId, string message, bool isRead = false)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notif = new Notification(recipientUserId, message, NotificationType.Manual);
        if (isRead)
            notif.MarkRead();
        db.Notifications.Add(notif);
        await db.SaveChangesAsync();
        return notif.Id;
    }

    // -----------------------------------------------------------------------
    // 401 — Anonim erişim
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetNotifications_Anonymous_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/notifications");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUnreadCount_Anonymous_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/notifications/unread-count");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MarkAllRead_Anonymous_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsync("/api/notifications/mark-all-read", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Send_Anonymous_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/api/notifications/send",
            new SendNotificationRequest(Guid.NewGuid(), null, "Test"), JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // -----------------------------------------------------------------------
    // GET /api/notifications — 200, per-kullanıcı kapsam
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetNotifications_Authenticated_Returns200WithOwnNotifications()
    {
        var umurAuth = await LoginAsync(UmurEmail, OrgPassword);
        var umurUserId = await GetUserIdAsync(UmurEmail);

        // Umur'a bildirim ekle
        await SeedNotificationAsync(umurUserId, "Umur bildirimi");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", umurAuth.AccessToken);

        var response = await _client.GetAsync("/api/notifications");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponse<IReadOnlyList<NotificationDto>>>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Success.ShouldBeTrue();
        payload.Data.ShouldNotBeNull();
        payload.Data!.ShouldContain(n => n.Message == "Umur bildirimi");
    }

    [Fact]
    public async Task GetNotifications_UserSeesOnlyOwnNotifications_NotOthers()
    {
        var avniyeAuth = await LoginAsync(AvniyeEmail, OrgPassword);
        var avniyeUserId = await GetUserIdAsync(AvniyeEmail);
        var muhammedUserId = await GetUserIdAsync(MuhammedEmail);

        // Her iki kullanıcıya farklı bildirimler ekle
        var avniyeMsg = $"Avniye özel {Guid.NewGuid()}";
        var muhammedMsg = $"Muhammed özel {Guid.NewGuid()}";
        await SeedNotificationAsync(avniyeUserId, avniyeMsg);
        await SeedNotificationAsync(muhammedUserId, muhammedMsg);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", avniyeAuth.AccessToken);

        var response = await _client.GetAsync("/api/notifications");
        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponse<IReadOnlyList<NotificationDto>>>(JsonOptions);

        payload!.Data!.ShouldContain(n => n.Message == avniyeMsg);
        payload.Data!.ShouldNotContain(n => n.Message == muhammedMsg);
    }

    // -----------------------------------------------------------------------
    // POST /{id}/read — tekil okundu, başkasının bildirimi 404
    // -----------------------------------------------------------------------

    [Fact]
    public async Task MarkRead_OwnNotification_Returns200AndSetsRead()
    {
        var avniyeAuth = await LoginAsync(AvniyeEmail, OrgPassword);
        var avniyeUserId = await GetUserIdAsync(AvniyeEmail);
        var notifId = await SeedNotificationAsync(avniyeUserId, "Okunacak bildirim");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", avniyeAuth.AccessToken);

        var response = await _client.PostAsync($"/api/notifications/{notifId}/read", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // DB'de gerçekten işaretlenmiş olmalı
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notif = await db.Notifications.FindAsync(notifId);
        notif!.IsRead.ShouldBeTrue();
    }

    [Fact]
    public async Task MarkRead_AnotherUsersNotification_Returns404()
    {
        var avniyeAuth = await LoginAsync(AvniyeEmail, OrgPassword);
        var muhammedUserId = await GetUserIdAsync(MuhammedEmail);

        // Muhammed'in bildirimi DB'de var; Avniye erişmeye çalışıyor
        var notifId = await SeedNotificationAsync(muhammedUserId, "Muhammed'in bildirimi");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", avniyeAuth.AccessToken);

        var response = await _client.PostAsync($"/api/notifications/{notifId}/read", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Muhammed'in bildirimi okunmamış kalmalı
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notif = await db.Notifications.FindAsync(notifId);
        notif!.IsRead.ShouldBeFalse("Başka kullanıcının bildirimi etkilenmemeli");
    }

    [Fact]
    public async Task MarkRead_NonExistentNotification_Returns404()
    {
        var avniyeAuth = await LoginAsync(AvniyeEmail, OrgPassword);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", avniyeAuth.AccessToken);

        var response = await _client.PostAsync($"/api/notifications/{Guid.NewGuid()}/read", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // -----------------------------------------------------------------------
    // Per-alıcı izolasyon: A'nın okuması B'yi etkilemez
    // -----------------------------------------------------------------------

    [Fact]
    public async Task MarkRead_AReadingNotification_DoesNotAffectBsIsolatedNotification()
    {
        var avniyeUserId = await GetUserIdAsync(AvniyeEmail);
        var muhammedUserId = await GetUserIdAsync(MuhammedEmail);

        // Aynı mesaj ama her biri için ayrı bildirim satırı (per-alıcı)
        var avniyeNotifId = await SeedNotificationAsync(avniyeUserId, "Paylaşımlı duyuru");
        var muhammedNotifId = await SeedNotificationAsync(muhammedUserId, "Paylaşımlı duyuru");

        // Avniye kendi bildirimini okur
        var avniyeAuth = await LoginAsync(AvniyeEmail, OrgPassword);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", avniyeAuth.AccessToken);
        var markResponse = await _client.PostAsync($"/api/notifications/{avniyeNotifId}/read", null);
        markResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Muhammed'in bildirimi okunmamış kalmalı
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var avniyeNotif = await db.Notifications.FindAsync(avniyeNotifId);
        var muhammedNotif = await db.Notifications.FindAsync(muhammedNotifId);
        avniyeNotif!.IsRead.ShouldBeTrue();
        muhammedNotif!.IsRead.ShouldBeFalse("B'nin bildirimi A'nın okumasından etkilenmemeli");
    }

    // -----------------------------------------------------------------------
    // POST /mark-all-read — yalnız kendi bildirimleri
    // -----------------------------------------------------------------------

    [Fact]
    public async Task MarkAllRead_Returns200_AndOnlyMarksCurrentUsersNotifications()
    {
        var avniyeUserId = await GetUserIdAsync(AvniyeEmail);
        var muhammedUserId = await GetUserIdAsync(MuhammedEmail);

        var avniyeNotifId = await SeedNotificationAsync(avniyeUserId, "Avniye okunacak");
        var muhammedNotifId = await SeedNotificationAsync(muhammedUserId, "Muhammed okunmamalı");

        var avniyeAuth = await LoginAsync(AvniyeEmail, OrgPassword);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", avniyeAuth.AccessToken);

        var response = await _client.PostAsync("/api/notifications/mark-all-read", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var avniyeNotif = await db.Notifications.FindAsync(avniyeNotifId);
        var muhammedNotif = await db.Notifications.FindAsync(muhammedNotifId);
        avniyeNotif!.IsRead.ShouldBeTrue();
        muhammedNotif!.IsRead.ShouldBeFalse("mark-all-read başka kullanıcıyı etkilememeli");
    }

    // -----------------------------------------------------------------------
    // POST /send — yetki: Admin/yönetici 200, Sales 403
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Send_AsAdmin_Returns200()
    {
        var umurAuth = await LoginAsync(UmurEmail, OrgPassword);
        var muhammedEmpId = await GetEmployeeIdAsync(MuhammedEmail);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", umurAuth.AccessToken);

        var request = new SendNotificationRequest(muhammedEmpId, "Test Başlık", "Admin bildirimi");
        var response = await _client.PostAsJsonAsync("/api/notifications/send", request, JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Send_AsManager_Returns200()
    {
        // Avniye yönetici — Muhammed'e gönderebilir
        var avniyeAuth = await LoginAsync(AvniyeEmail, OrgPassword);
        var muhammedEmpId = await GetEmployeeIdAsync(MuhammedEmail);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", avniyeAuth.AccessToken);

        var request = new SendNotificationRequest(muhammedEmpId, null, "Yönetici bildirimi");
        var response = await _client.PostAsJsonAsync("/api/notifications/send", request, JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Send_AsSalesUser_Returns403()
    {
        // Muhammed Sales kullanıcısı; astı yok → bildirim gönderemez
        var muhammedAuth = await LoginAsync(MuhammedEmail, OrgPassword);
        var umurEmpId = await GetEmployeeIdAsync(UmurEmail);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", muhammedAuth.AccessToken);

        var request = new SendNotificationRequest(umurEmpId, null, "Sales bildirimi");
        var response = await _client.PostAsJsonAsync("/api/notifications/send", request, JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Send_AdminCrossUnit_SendsToSubtreeUsersExcludingSender()
    {
        // Umur (Admin), Avniye'nin birimindeki kullanıcılara (Muhammed) gönderir
        var umurAuth = await LoginAsync(UmurEmail, OrgPassword);
        var umurUserId = await GetUserIdAsync(UmurEmail);
        var muhammedUserId = await GetUserIdAsync(MuhammedEmail);
        var avniyeEmpId = await GetEmployeeIdAsync(AvniyeEmail);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", umurAuth.AccessToken);

        var uniqueMsg = $"Çapraz birim bildirimi {Guid.NewGuid()}";
        var request = new SendNotificationRequest(avniyeEmpId, "Başlık", uniqueMsg);
        var sendResponse = await _client.PostAsJsonAsync("/api/notifications/send", request, JsonOptions);
        sendResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Muhammed bildirimi almış olmalı
        var muhammedAuth = await LoginAsync(MuhammedEmail, OrgPassword);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", muhammedAuth.AccessToken);
        var getMineResponse = await _client.GetAsync("/api/notifications");
        var payload = await getMineResponse.Content
            .ReadFromJsonAsync<ApiResponse<IReadOnlyList<NotificationDto>>>(JsonOptions);
        payload!.Data!.ShouldContain(n => n.Message == uniqueMsg);

        // Gönderen (Umur) kendi bildirim listesinde bu mesajı görmemeli
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var umurNotif = await db.Notifications
            .Where(n => n.RecipientUserId == umurUserId && n.Message == uniqueMsg)
            .FirstOrDefaultAsync();
        umurNotif.ShouldBeNull("Gönderen kendi bildirimini almamalı");
    }

    // -----------------------------------------------------------------------
    // POST /send — Type=Manual ve SenderName dolu olmalı
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Send_CreatedNotificationsHaveManualTypeAndSenderName()
    {
        var avniyeAuth = await LoginAsync(AvniyeEmail, OrgPassword);
        var muhammedEmpId = await GetEmployeeIdAsync(MuhammedEmail);
        var muhammedUserId = await GetUserIdAsync(MuhammedEmail);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", avniyeAuth.AccessToken);

        var uniqueMsg = $"Manual type test {Guid.NewGuid()}";
        var request = new SendNotificationRequest(muhammedEmpId, "Başlık", uniqueMsg);
        var response = await _client.PostAsJsonAsync("/api/notifications/send", request, JsonOptions);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notif = await db.Notifications
            .FirstOrDefaultAsync(n => n.RecipientUserId == muhammedUserId && n.Message == uniqueMsg);
        notif.ShouldNotBeNull();
        notif!.Type.ShouldBe(NotificationType.Manual);
        notif.SenderName.ShouldNotBeNullOrWhiteSpace("SenderName dolu olmalı");
    }

    // -----------------------------------------------------------------------
    // GET /unread-count
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetUnreadCount_Returns200WithCorrectCount()
    {
        var avniyeAuth = await LoginAsync(AvniyeEmail, OrgPassword);
        var avniyeUserId = await GetUserIdAsync(AvniyeEmail);

        // 2 okunmamış + 1 okunmuş bildirim ekle
        await SeedNotificationAsync(avniyeUserId, $"Okunmamış 1 {Guid.NewGuid()}");
        await SeedNotificationAsync(avniyeUserId, $"Okunmamış 2 {Guid.NewGuid()}");
        await SeedNotificationAsync(avniyeUserId, $"Okunmuş {Guid.NewGuid()}", isRead: true);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", avniyeAuth.AccessToken);

        var response = await _client.GetAsync("/api/notifications/unread-count");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<int>>(JsonOptions);
        payload!.Data.ShouldBeGreaterThanOrEqualTo(2);
    }
}

/// <summary>Integration testleri için SignalR hub bağlantısı olmadan çalışan no-op notifier.</summary>
file sealed class NoOpRealtimeNotifier : IRealtimeNotifier
{
    public Task NotifyUsersAsync(
        IEnumerable<Guid> userIds,
        Oypa.Crm.Contracts.Notifications.NotificationDto payload,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
