namespace Oypa.Crm.Api.Middlewares;

/// <summary>Temel güvenlik yanıt başlıklarını ekler.</summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    // Swagger UI aynı origin'de sunulduğundan ve inline script/style kullandığından
    // 'unsafe-inline' verilmek zorunda; production'da Swagger kaldırılırsa bu direktif sıkılaştırılabilir.
    private const string CspPolicy =
        "default-src 'self'; " +
        "img-src 'self' data:; " +
        "style-src 'self' 'unsafe-inline'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "frame-ancestors 'none'; " +
        "object-src 'none'; " +
        "base-uri 'self'";

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["X-XSS-Protection"] = "0";
        headers["Content-Security-Policy"] = CspPolicy;
        await next(context);
    }
}
