using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Oypa.Crm.Api.BackgroundServices;
using Oypa.Crm.Api.Common;
using Oypa.Crm.Api.Extensions;
using Oypa.Crm.Api.Filters;
using Oypa.Crm.Api.HealthChecks;
using Oypa.Crm.Api.Hubs;
using Oypa.Crm.Api.Middlewares;
using Oypa.Crm.Application;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Contracts.Common;
using Oypa.Crm.Infrastructure;
using Oypa.Crm.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "frontend";

builder.Services
    .AddControllers(options => options.Filters.Add<ValidationFilter>())
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        // DateTime'ları UTC 'Z' ile serileştir (istemci doğru yerel saate çevirsin).
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
    });

// Model binding hatalarını da standart ApiResponse zarfına çevir.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .SelectMany(kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage))
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .ToList();
        // Alan-bazlı hatalar: ModelState anahtarı camelCase'e çevrilir.
        var fieldErrors = context.ModelState
            .Where(kvp => kvp.Value!.Errors.Count > 0 && !string.IsNullOrEmpty(kvp.Key))
            .ToDictionary(
                kvp => char.IsLower(kvp.Key[0]) ? kvp.Key : char.ToLowerInvariant(kvp.Key[0]) + kvp.Key[1..],
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
        return new BadRequestObjectResult(ApiResponse.Fail("Geçersiz istek.", errors, fieldErrors));
    };
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUserService>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAppRateLimiting();
builder.Services.AddAppSwagger();

// Sağlık kontrolü: liveness + DB hazırlık kontrolü (ek NuGet paketi gerektirmez).
builder.Services.AddHealthChecks()
    .AddCheck<DbHealthCheck>("db");

// SignalR — custom IUserIdProvider: sub claim → string UserId
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, NotificationHubUserIdProvider>();

// IRealtimeNotifier implementasyonu (Api katmanında, Clean Architecture)
builder.Services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();

// Yaklaşan ihale bildirimi — periyodik BackgroundService
builder.Services.AddHostedService<UpcomingTenderReminderHostedService>();

// Haftalık hedef snapshot — her 6 saatte bir tüm aktif hedefler için snapshot oluşturur
builder.Services.AddHostedService<WeeklyGoalSnapshotHostedService>();

// Süresi dolmuş RefreshToken temizleme — günde bir kez çalışır
builder.Services.AddHostedService<RefreshTokenCleanupHostedService>();

// JWT Bearer — /hubs/notifications için query-string token desteği
builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.Events ??= new JwtBearerEvents();
    var originalOnMessageReceived = options.Events.OnMessageReceived;
    options.Events.OnMessageReceived = async context =>
    {
        if (originalOnMessageReceived is not null)
            await originalOnMessageReceived(context);

        // SignalR WebSocket/SSE bağlantısı: token query-string'den al
        if (string.IsNullOrEmpty(context.Token))
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/hubs/notifications", StringComparison.OrdinalIgnoreCase))
            {
                context.Token = accessToken;
            }
        }
    };
});

builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    policy
        .WithOrigins(origins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials(); // SignalR WebSocket/SSE için credentials gerekli
}));

var app = builder.Build();

// CorrelationIdMiddleware pipeline'ın EN BAŞINDA yer alır; tüm middleware'ler
// ve controller log satırları CorrelationId scope'undan faydalanır.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

// Swagger her ortamda açık. Production'da UI kök adreste (/) sunulur (talep).
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    // JSON tanımını mutlak yoldan ver; RoutePrefix boşken (kök) göreli yol
    // "/v1/swagger.json"a çözülüp 404 verir. Mutlak yol her iki ortamda da doğru.
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Oypa.Crm.Api v1");
    if (!app.Environment.IsDevelopment())
        options.RoutePrefix = string.Empty; // Production: Swagger UI index (kök)
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseCors(CorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// SignalR hub endpoint'i
app.MapHub<NotificationsHub>("/hubs/notifications");

// Test ortamında migration/seed atlanır (integration testleri kendi sağlayıcısını kurar).
if (!app.Environment.IsEnvironment("Testing"))
{
    try
    {
        await DbSeeder.SeedAsync(app.Services);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Veritabanı seed/migration sırasında hata oluştu.");
    }
}

app.Run();

public partial class Program;
