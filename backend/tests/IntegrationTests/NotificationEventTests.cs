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
using Oypa.Crm.Contracts.Meetings;
using Oypa.Crm.Contracts.Notifications;
using Oypa.Crm.Contracts.SalesReps;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Oypa.Crm.Infrastructure.Identity;
using Oypa.Crm.Infrastructure.Persistence;
using Shouldly;

namespace Oypa.Crm.IntegrationTests;

/// <summary>
/// Olay-tetikli bildirim testleri:
/// MeetingScheduledEvent → yönetici zincirine bildirim üretildiğini doğrular.
/// </summary>
public sealed class NotificationEventTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    // Org hiyerarşisi: UmurEvt (kök, Admin) → AvniyeEvt (yönetici, Admin) → MuhammedEvt (Sales, yaprak)
    private const string UmurEmail = "evtnotif.umur@oypa.com.tr";
    private const string AvniyeEmail = "evtnotif.avniye@oypa.com.tr";
    private const string MuhammedEmail = "evtnotif.muhammed@oypa.com.tr";
    private const string OrgPassword = "Oypa!2026";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public NotificationEventTests(CustomWebApplicationFactory factory)
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

                // SignalR no-op stub
                var existing = services.FirstOrDefault(d => d.ServiceType == typeof(IRealtimeNotifier));
                if (existing is not null)
                    services.Remove(existing);
                services.AddScoped<IRealtimeNotifier>(_ => new EventTestNoOpNotifier());
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

        await SeedOrgAndRepAsync(db, userManager);
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
            UserName = email, Email = email, FullName = fullName, EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, role);
    }

    private static async Task SeedOrgAndRepAsync(
        AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        if (await db.Employees.AnyAsync(e => e.Email == UmurEmail))
            return;

        // Umur (kök) → Avniye (yönetici) → Muhammed (satış uzmanı)
        var umurUser = await CreateOrGetUserAsync(userManager, UmurEmail, "EvtNotif Umur", OrgPassword, "Admin");
        var avniyeUser = await CreateOrGetUserAsync(userManager, AvniyeEmail, "EvtNotif Avniye", OrgPassword, "Admin");
        var muhammedUser = await CreateOrGetUserAsync(userManager, MuhammedEmail, "EvtNotif Muhammed", OrgPassword, "Sales");

        var umur = new Employee("Satış Direktörü", "EvtNotif Umur", UmurEmail);
        umur.LinkAccount(umurUser.Id);
        db.Employees.Add(umur);
        await db.SaveChangesAsync();

        var avniye = new Employee("Satış Müdürü", "EvtNotif Avniye", AvniyeEmail, umur.Id);
        avniye.LinkAccount(avniyeUser.Id);
        db.Employees.Add(avniye);
        await db.SaveChangesAsync();

        var muhammed = new Employee("Satış Uzmanı", "EvtNotif Muhammed", MuhammedEmail, avniye.Id);
        muhammed.LinkAccount(muhammedUser.Id);
        db.Employees.Add(muhammed);
        await db.SaveChangesAsync();

        // Muhammed'e bağlı bir SalesRep oluştur
        var rep = new SalesRep("EvtNotif Temsilci", MuhammedEmail);
        rep.LinkEmployee(muhammed.Id);
        db.SalesReps.Add(rep);
        await db.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> CreateOrGetUserAsync(
        UserManager<ApplicationUser> userManager,
        string email, string fullName, string password, string role)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            return existing;

        var user = new ApplicationUser
        {
            UserName = email, Email = email, FullName = fullName, EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, role);
        return (await userManager.FindByEmailAsync(email))!;
    }

    private async Task<AuthResponse> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, password), JsonOptions);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"{email} ile giriş başarısız");
        return (await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions))!.Data!;
    }

    private async Task<Guid> GetUserIdAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return (await userManager.FindByEmailAsync(email))!.Id;
    }

    // -----------------------------------------------------------------------
    // MeetingScheduledEvent → yönetici zincirine bildirim üretilir
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ScheduleMeeting_TriggersNotificationForManagerChain()
    {
        // Bir görüşme planlandığında Muhammed'in yönetici zinciri (Avniye, Umur)
        // ve Muhammed'in kendisi bildirim almalı.

        var umurAuth = await LoginAsync(UmurEmail, OrgPassword);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", umurAuth.AccessToken);

        // Rep ve firma al
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var repExists = await db.SalesReps.AnyAsync(r => r.Email == MuhammedEmail);
            repExists.ShouldBeTrue("SalesRep seed edilmiş olmalı");
        }

        var umurUserId = await GetUserIdAsync(UmurEmail);
        var avniyeUserId = await GetUserIdAsync(AvniyeEmail);
        var muhammedUserId = await GetUserIdAsync(MuhammedEmail);

        // Şirket oluştur
        var company = await _client.PostAsJsonAsync("/api/companies",
            new Oypa.Crm.Contracts.Companies.CreateCompanyRequest(
                $"Evt Test A.Ş. {Guid.NewGuid()}", Sector.Energy, "0212", "evt@test.com", "Adres"),
            JsonOptions);
        company.StatusCode.ShouldBe(HttpStatusCode.Created);
        var companyData = (await company.Content
            .ReadFromJsonAsync<ApiResponse<Oypa.Crm.Contracts.Companies.CompanyDto>>(JsonOptions))!.Data!;

        // SalesRep Id'sini bul
        Guid repId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var rep = await db.SalesReps.FirstAsync(r => r.Email == MuhammedEmail);
            repId = rep.Id;
        }

        // Görüşme planla — bu MeetingScheduledEvent tetikler
        var scheduleResponse = await _client.PostAsJsonAsync("/api/meetings",
            new ScheduleMeetingRequest(
                companyData.Id, null, repId,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
                new TimeOnly(10, 0), "Evt Test Adresi", MeetingMethod.Visit),
            JsonOptions);
        scheduleResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Yönetici zincirindeki kullanıcılar (Umur, Avniye) ve Muhammed bildirim almış olmalı
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var umurNotifs = await db.Notifications
                .Where(n => n.RecipientUserId == umurUserId
                         && n.Type == NotificationType.MeetingScheduled)
                .ToListAsync();
            var avniyeNotifs = await db.Notifications
                .Where(n => n.RecipientUserId == avniyeUserId
                         && n.Type == NotificationType.MeetingScheduled)
                .ToListAsync();

            // Umur (kök) yönetici olarak bildirim almalı
            umurNotifs.Count.ShouldBeGreaterThan(0, "Umur (kök yönetici) MeetingScheduled bildirimi almalı");
            // Avniye (ara yönetici) bildirim almalı
            avniyeNotifs.Count.ShouldBeGreaterThan(0, "Avniye (yönetici) MeetingScheduled bildirimi almalı");
        }
    }

    [Fact]
    public async Task ScheduleMeeting_RepLinkedEmployee_NotificationHasCorrectLink()
    {
        var umurAuth = await LoginAsync(UmurEmail, OrgPassword);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", umurAuth.AccessToken);

        var umurUserId = await GetUserIdAsync(UmurEmail);

        var company = await _client.PostAsJsonAsync("/api/companies",
            new Oypa.Crm.Contracts.Companies.CreateCompanyRequest(
                $"Link Test A.Ş. {Guid.NewGuid()}", Sector.Retail, "0212", "link@test.com", "Adres"),
            JsonOptions);
        company.StatusCode.ShouldBe(HttpStatusCode.Created);
        var companyData = (await company.Content
            .ReadFromJsonAsync<ApiResponse<Oypa.Crm.Contracts.Companies.CompanyDto>>(JsonOptions))!.Data!;

        Guid repId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var rep = await db.SalesReps.FirstAsync(r => r.Email == MuhammedEmail);
            repId = rep.Id;
        }

        var scheduleResponse = await _client.PostAsJsonAsync("/api/meetings",
            new ScheduleMeetingRequest(
                companyData.Id, null, repId,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(4)),
                new TimeOnly(11, 0), "Link Test Adresi", MeetingMethod.Phone),
            JsonOptions);
        scheduleResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Umur'un bildirimi doğru link içermeli
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notif = await db.Notifications
                .Where(n => n.RecipientUserId == umurUserId
                         && n.Type == NotificationType.MeetingScheduled
                         && n.Link != null && n.Link.Contains(companyData.Id.ToString()))
                .FirstOrDefaultAsync();

            notif.ShouldNotBeNull("Bildirim /companies/{companyId} linkini içermeli");
        }
    }
}

/// <summary>Olay testi için SignalR no-op stub.</summary>
file sealed class EventTestNoOpNotifier : IRealtimeNotifier
{
    public Task NotifyUsersAsync(
        IEnumerable<Guid> userIds,
        NotificationDto payload,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
