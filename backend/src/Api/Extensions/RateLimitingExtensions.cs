using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Oypa.Crm.Contracts.Common;

namespace Oypa.Crm.Api.Extensions;

public static class RateLimitingExtensions
{
    public const string AuthLogin = "auth-login";
    public const string AuthRefresh = "auth-refresh";
    public const string Search = "urun-arama";
    public const string AdminSensitive = "admin-sensitive";

    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services, bool isDevelopment = false)
    {
        // Development/E2E ortamında throttle pratikte devre dışı (çok yüksek limit);
        // production'da sıkı limitler korunur. Böylece lokal test/otomasyon engellenmez.
        var loginLimit = isDevelopment ? 100_000 : 5;
        var refreshLimit = isDevelopment ? 100_000 : 10;
        var searchLimit = isDevelopment ? 100_000 : 30;
        var adminLimit = isDevelopment ? 100_000 : 5;

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Login: brute-force koruması — IP başına düşük limit.
            // NOT: Email bazlı partition, request body okumayı gerektirdiğinden middleware katmanında
            // pratik değildir. Birincil hesap kilitleme savunması IdentityService.ValidateCredentialsAsync
            // içindeki Identity Lockout mekanizmasıyla sağlanmaktadır (5 başarısız giriş → 5 dk kilit).
            // Bu kural NAT/proxy arkasındaki kitlesel saldırılara karşı ek bir IP katmanı sunar.
            options.AddPolicy(AuthLogin, ctx => RateLimitPartition.GetFixedWindowLimiter(
                ClientIp(ctx),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = loginLimit, Window = TimeSpan.FromMinutes(1) }));

            // Refresh: token abuse koruması — IP başına.
            options.AddPolicy(AuthRefresh, ctx => RateLimitPartition.GetFixedWindowLimiter(
                ClientIp(ctx),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = refreshLimit, Window = TimeSpan.FromMinutes(1) }));

            // Yoğun sorgu/arama: kullanıcı (veya IP) başına sliding window.
            options.AddPolicy(Search, ctx => RateLimitPartition.GetSlidingWindowLimiter(
                UserOrIp(ctx),
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = searchLimit,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6
                }));

            // Kritik admin işlemleri: çok düşük limit, kullanıcı başına.
            options.AddPolicy(AdminSensitive, ctx => RateLimitPartition.GetFixedWindowLimiter(
                UserOrIp(ctx),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = adminLimit, Window = TimeSpan.FromMinutes(5) }));

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsJsonAsync(
                    ApiResponse.Fail("Çok fazla istek. Lütfen daha sonra tekrar deneyin."), token);
            };
        });

        return services;
    }

    private static string ClientIp(HttpContext ctx) =>
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static string UserOrIp(HttpContext ctx) =>
        ctx.User.FindFirst("sub")?.Value
        ?? ctx.Connection.RemoteIpAddress?.ToString()
        ?? "anonymous";
}
