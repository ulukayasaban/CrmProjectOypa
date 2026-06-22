namespace Oypa.Crm.Contracts.Auth;

/// <summary>Yönetici tarafından yeni kullanıcı oluşturma isteği.</summary>
public sealed record RegisterUserRequest(
    string Email,
    string Password,
    string FullName,
    string Role);
