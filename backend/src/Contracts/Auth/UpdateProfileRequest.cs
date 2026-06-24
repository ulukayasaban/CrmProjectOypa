namespace Oypa.Crm.Contracts.Auth;

/// <summary>Kimliği doğrulanmış kullanıcının kendi profil bilgilerini güncelleme isteği.</summary>
public sealed record UpdateProfileRequest(string FullName, string? Phone, string? Position);
