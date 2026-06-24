namespace Oypa.Crm.Application.Common.Interfaces;

/// <summary>
/// E-posta gönderim altyapısını Application katmanından soyutlar.
/// Gerçek SMTP ve geliştirme ortamı için farklı implementasyonlar kullanılır.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Belirtilen alıcıya HTML formatında e-posta gönderir.
    /// </summary>
    /// <param name="to">Alıcı e-posta adresi.</param>
    /// <param name="subject">E-posta konusu.</param>
    /// <param name="htmlBody">HTML içerik gövdesi.</param>
    /// <param name="ct">İptal jetonu.</param>
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
