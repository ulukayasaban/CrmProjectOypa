namespace Oypa.Crm.Contracts.Auth;

/// <summary>Kimliği doğrulanmış kullanıcının parolasını değiştirme isteği.</summary>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
