using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Common.Options;

namespace Oypa.Crm.Infrastructure.Email;

/// <summary>
/// System.Net.Mail.SmtpClient kullanarak gerçek e-posta gönderimini sağlar.
/// Yapılandırma <see cref="EmailOptions"/>'tan okunur; kimlik bilgileri
/// environment değişkenlerinden enjekte edilir.
/// </summary>
public sealed class SmtpEmailSender(
    IOptions<EmailOptions> emailOptions,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = emailOptions.Value;

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        // SmtpClient Dispose çağrısı bağlantıyı düzgün kapatır.
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = new NetworkCredential(_options.User, _options.Password)
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_options.From, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        message.To.Add(to);

        logger.LogInformation("E-posta gönderiliyor: {To} / {Subject}", to, subject);

        // SmtpClient Task tabanlı API sağlar; CancellationToken için Task.Run sargısı kullanılır.
        await Task.Run(() => client.Send(message), ct);

        logger.LogInformation("E-posta gönderildi: {To} / {Subject}", to, subject);
    }
}
