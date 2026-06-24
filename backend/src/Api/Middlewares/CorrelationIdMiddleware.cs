namespace Oypa.Crm.Api.Middlewares;

/// <summary>
/// Her isteğe bir korelasyon kimliği atar.
/// <list type="bullet">
///   <item>Gelen <c>X-Correlation-Id</c> header'ı varsa kullanılır.</item>
///   <item>Yoksa <see cref="HttpContext.TraceIdentifier"/> değeri alınır; o da boşsa yeni bir GUID üretilir.</item>
/// </list>
/// Değer hem <c>HttpContext.Items["CorrelationId"]</c>'ye hem de yanıt <c>X-Correlation-Id</c>
/// header'ına yazılır ve loglama scope'una eklenir; böylece aynı isteğe ait log satırları
/// filtrelerekbulunabilir.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    /// <summary>HttpContext.Items'ta ve loglama scope'unda kullanılan anahtar.</summary>
    public const string CorrelationIdKey = "CorrelationId";

    /// <summary>İstekten okunan / yanıta yazılan HTTP header adı.</summary>
    public const string CorrelationIdHeader = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        // Öncelik: gelen header → TraceIdentifier → yeni GUID
        var correlationId = GetOrCreateCorrelationId(context);

        // Sonraki middleware'lerin Items üzerinden erişebilmesi için sakla.
        context.Items[CorrelationIdKey] = correlationId;

        // İstemcinin yanıtta görebilmesi için response header'a yaz.
        // Header'ın body akışından önce eklenmesi gerektiğinden buraya alınır.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        // Loglama scope'u: bu middleware'den sonra gelen tüm log satırları
        // CorrelationId alanını otomatik olarak taşır.
        using (logger.BeginScope(new Dictionary<string, object>
        {
            [CorrelationIdKey] = correlationId
        }))
        {
            await next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue.ToString();
        }

        // TraceIdentifier ASP.NET Core tarafından atanır; boş kalması beklenmez ama null-guard eklenir.
        if (!string.IsNullOrWhiteSpace(context.TraceIdentifier))
            return context.TraceIdentifier;

        return Guid.NewGuid().ToString("D");
    }
}
