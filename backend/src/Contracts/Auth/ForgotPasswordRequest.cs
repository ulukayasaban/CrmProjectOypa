namespace Oypa.Crm.Contracts.Auth;

/// <summary>Parola sıfırlama bağlantısı isteği (anonim uç için).</summary>
public sealed record ForgotPasswordRequest(string Email);
