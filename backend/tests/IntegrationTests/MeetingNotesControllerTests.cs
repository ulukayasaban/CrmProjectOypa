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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Oypa.Crm.Contracts.Auth;
using Oypa.Crm.Contracts.Common;
using Oypa.Crm.Contracts.Meetings;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Oypa.Crm.Infrastructure.Identity;
using Oypa.Crm.Infrastructure.Persistence;
using Shouldly;

namespace Oypa.Crm.IntegrationTests;

/// <summary>
/// POST /api/meetings/{id}/notes ve GET /api/reports/meetings endpoint testleri.
/// </summary>
public sealed class MeetingNotesControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string AdminEmail = CustomWebApplicationFactory.AdminEmail;
    private const string AdminPassword = CustomWebApplicationFactory.AdminPassword;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public MeetingNotesControllerTests(CustomWebApplicationFactory factory)
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

    /// <summary>DB'ye doğrudan görüşme ekler; navigation property'ler olmaksızın.</summary>
    private async Task<Guid> SeedMeetingAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var company = new Company("Test Firma", Sector.Retail, "1", "firma@test.com", "Test Adres");
        db.Companies.Add(company);

        var rep = new SalesRep("Test Temsilci", "temsilci@oypa.com");
        db.SalesReps.Add(rep);

        await db.SaveChangesAsync();

        var meeting = Meeting.Schedule(
            company.Id, rep.Id, null,
            new DateOnly(2026, 6, 11), new TimeOnly(10, 0), "Adres", MeetingMethod.Visit);
        db.Meetings.Add(meeting);
        await db.SaveChangesAsync();

        return meeting.Id;
    }

    // -----------------------------------------------------------------------
    // POST /api/meetings/{id}/notes — yetkilendirme
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PostNote_Anonymous_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync(
            $"/api/meetings/{Guid.NewGuid()}/notes",
            new AddMeetingNoteRequest("İçerik"), JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // -----------------------------------------------------------------------
    // POST /api/meetings/{id}/notes — bilinmeyen id
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PostNote_UnknownMeetingId_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        SetBearer(token);

        var response = await _client.PostAsJsonAsync(
            $"/api/meetings/{Guid.NewGuid()}/notes",
            new AddMeetingNoteRequest("İçerik"), JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // -----------------------------------------------------------------------
    // POST /api/meetings/{id}/notes — başarılı not ekleme
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PostNote_ValidMeeting_Returns200AndNoteAppearsInDto()
    {
        var meetingId = await SeedMeetingAsync();

        var token = await LoginAndGetTokenAsync();
        SetBearer(token);

        var response = await _client.PostAsJsonAsync(
            $"/api/meetings/{meetingId}/notes",
            new AddMeetingNoteRequest("Toplantı verimli geçti."), JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<MeetingDto>>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Success.ShouldBeTrue();
        payload.Data.ShouldNotBeNull();
        payload.Data!.Notes.ShouldNotBeEmpty();
        payload.Data.Notes.Any(n => n.Content == "Toplantı verimli geçti.").ShouldBeTrue();
    }

    [Fact]
    public async Task PostNote_ValidMeeting_NoteAppearsInGetAllMeetings()
    {
        var meetingId = await SeedMeetingAsync();

        var token = await LoginAndGetTokenAsync();
        SetBearer(token);

        // Not ekle
        var addResponse = await _client.PostAsJsonAsync(
            $"/api/meetings/{meetingId}/notes",
            new AddMeetingNoteRequest("GET ile görünmeli"), JsonOptions);
        addResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Tüm toplantıları getir
        var getAllResponse = await _client.GetAsync("/api/meetings");
        getAllResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var allPayload = await getAllResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<MeetingDto>>>(JsonOptions);
        allPayload.ShouldNotBeNull();

        var meeting = allPayload!.Data!.FirstOrDefault(m => m.Id == meetingId);
        meeting.ShouldNotBeNull("Görüşme GET /meetings sonucunda görünmeli");
        meeting!.Notes.Any(n => n.Content == "GET ile görünmeli").ShouldBeTrue();
    }

    [Fact]
    public async Task PostNote_AuthorNameIsPopulatedFromAuthenticatedUser()
    {
        var meetingId = await SeedMeetingAsync();

        var token = await LoginAndGetTokenAsync();
        SetBearer(token);

        var response = await _client.PostAsJsonAsync(
            $"/api/meetings/{meetingId}/notes",
            new AddMeetingNoteRequest("Yazar bilgisi notta olmalı"), JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<MeetingDto>>(JsonOptions);

        var note = payload!.Data!.Notes.Single(n => n.Content == "Yazar bilgisi notta olmalı");
        note.AuthorName.ShouldNotBeNullOrWhiteSpace("Yazar adı boş olmamalı");
    }

    // -----------------------------------------------------------------------
    // GET /api/reports/meetings — yetkilendirme
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetMeetingReport_Anonymous_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/reports/meetings");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // -----------------------------------------------------------------------
    // GET /api/reports/meetings — başarılı rapor
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetMeetingReport_Authenticated_Returns200WithXlsxContentType()
    {
        var token = await LoginAndGetTokenAsync();
        SetBearer(token);

        var response = await _client.GetAsync("/api/reports/meetings");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType
            .ShouldBe("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Fact]
    public async Task GetMeetingReport_Authenticated_ResponseBytesStartWithZipSignature()
    {
        var token = await LoginAndGetTokenAsync();
        SetBearer(token);

        var response = await _client.GetAsync("/api/reports/meetings");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.ShouldBeGreaterThan(4);
        bytes[0].ShouldBe((byte)0x50);
        bytes[1].ShouldBe((byte)0x4B);
        bytes[2].ShouldBe((byte)0x03);
        bytes[3].ShouldBe((byte)0x04);
    }
}
