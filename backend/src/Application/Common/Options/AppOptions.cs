namespace Oypa.Crm.Application.Common.Options;

/// <summary>
/// Uygulama geneli yapılandırma seçenekleri.
/// </summary>
public sealed class AppOptions
{
    public const string SectionName = "App";

    /// <summary>
    /// Frontend uygulamasının temel URL'si.
    /// Parola sıfırlama bağlantısı oluşturmak için kullanılır.
    /// </summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";
}
