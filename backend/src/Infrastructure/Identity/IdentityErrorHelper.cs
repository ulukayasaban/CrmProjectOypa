using Microsoft.AspNetCore.Identity;

namespace Oypa.Crm.Infrastructure.Identity;

/// <summary>
/// ASP.NET Identity hata kodlarını Türkçe kullanıcı-dostu mesajlara çevirir.
/// Bilinmeyen kodlar için Identity'nin orijinal Description'ı fallback olarak kullanılır.
/// </summary>
internal static class IdentityErrorHelper
{
    private static readonly IReadOnlyDictionary<string, string> TurkishMessages =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Parola politika hataları
            ["PasswordTooShort"]                 = "Parola en az 8 karakter olmalıdır.",
            ["PasswordRequiresDigit"]             = "Parola en az bir rakam içermelidir (0-9).",
            ["PasswordRequiresLower"]             = "Parola en az bir küçük harf içermelidir.",
            ["PasswordRequiresUpper"]             = "Parola en az bir büyük harf içermelidir.",
            ["PasswordRequiresNonAlphanumeric"]   = "Parola en az bir özel karakter içermelidir (!@#$ vb.).",
            ["PasswordRequiresUniqueChars"]       = "Parola yeterince farklı karakter içermelidir.",
            ["PasswordMismatch"]                  = "Mevcut parola hatalı.",

            // E-posta / kullanıcı adı çakışması
            ["DuplicateEmail"]                    = "Bu e-posta adresi zaten kayıtlıdır.",
            ["DuplicateUserName"]                 = "Bu kullanıcı adı zaten kullanımda.",
            ["InvalidEmail"]                      = "Geçersiz e-posta adresi.",
            ["InvalidUserName"]                   = "Geçersiz kullanıcı adı. Yalnızca harf, rakam ve @ . _ - karakterleri kullanılabilir.",

            // Kullanıcı işlemleri
            ["UserNotFound"]                      = "Kullanıcı bulunamadı.",
            ["UserAlreadyInRole"]                 = "Kullanıcı zaten bu roldedir.",
            ["UserNotInRole"]                     = "Kullanıcı bu rolde değildir.",
            ["UserAlreadyHasPassword"]            = "Kullanıcının zaten bir parolası var.",
            ["UserLockoutNotEnabled"]             = "Bu kullanıcı için kilitleme devre dışıdır.",
            ["UserLockedOut"]                     = "Hesap geçici olarak kilitlenmiştir.",

            // Token hataları
            ["InvalidToken"]                      = "Geçersiz veya süresi dolmuş güvenlik jetonu.",
            ["RecoveryCodeRedemptionFailed"]      = "Kurtarma kodu kullanılamadı.",

            // Rol hataları
            ["RoleNotFound"]                      = "Rol bulunamadı.",
            ["DuplicateRoleName"]                 = "Bu rol adı zaten mevcut.",
            ["InvalidRoleName"]                   = "Geçersiz rol adı.",

            // Genel
            ["ConcurrencyFailure"]                = "Eş zamanlılık hatası; lütfen tekrar deneyin.",
            ["DefaultError"]                      = "Bilinmeyen bir hata oluştu."
        };

    /// <summary>
    /// Identity hata kodunu Türkçe mesaja çevirir.
    /// Kod bulunamazsa <paramref name="fallbackDescription"/> döner.
    /// </summary>
    public static string Translate(string code, string fallbackDescription) =>
        TurkishMessages.TryGetValue(code, out var tr) ? tr : fallbackDescription;

    /// <summary>
    /// Identity hata koleksiyonunu Türkçe mesaj listesine çevirir.
    /// </summary>
    public static IReadOnlyList<string> Translate(IEnumerable<IdentityError> errors) =>
        errors.Select(e => Translate(e.Code, e.Description)).ToList();
}
