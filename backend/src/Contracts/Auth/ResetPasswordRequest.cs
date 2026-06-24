namespace Oypa.Crm.Contracts.Auth;

/// <summary>Parola sıfırlama tamamlama isteği; token e-posta bağlantısından gelir.</summary>
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
