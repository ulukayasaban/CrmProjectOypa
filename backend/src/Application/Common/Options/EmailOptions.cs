namespace Oypa.Crm.Application.Common.Options;

/// <summary>
/// SMTP e-posta yapılandırması. Gerçek kimlik bilgileri (User, Password)
/// environment değişkenleri veya user-secrets aracılığıyla sağlanır; repoya yazılmaz.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>SMTP sunucu adresi. Boşsa NullEmailSender devreye girer.</summary>
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    /// <summary>SMTP kimlik doğrulama kullanıcı adı (genellikle e-posta adresi).</summary>
    public string User { get; set; } = string.Empty;

    /// <summary>SMTP şifresi. Environment/user-secrets'tan gelir, appsettings'e yazılmaz.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Gönderen e-posta adresi.</summary>
    public string From { get; set; } = "no-reply@oypa.com.tr";

    /// <summary>Gönderen görünen adı.</summary>
    public string FromName { get; set; } = "OYPA CRM";

    public bool EnableSsl { get; set; } = true;
}
