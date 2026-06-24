using Microsoft.Extensions.Logging;
using Oypa.Crm.Application.Common.Interfaces;

namespace Oypa.Crm.Infrastructure.Email;

/// <summary>
/// SMTP yapılandırılmamış ortamlarda (geliştirme, CI) kullanılan sahte e-posta göndericisi.
/// Gerçek e-posta göndermez; yalnızca loglama yapar — böylece uygulama akışı SMTP olmadan da çalışır.
/// </summary>
public sealed class NullEmailSender(ILogger<NullEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        // SMTP sunucu tanımlanmamış; e-posta simüle edilerek loglanır.
        logger.LogInformation("E-posta simüle edildi: {To} / {Subject}", to, subject);
        return Task.CompletedTask;
    }
}
