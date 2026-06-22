namespace Oypa.Crm.Contracts.Employees;

/// <summary>Hesap oluşturma veya parola sıfırlama sonucunda döndürülen geçici kimlik bilgileri.</summary>
public sealed record AccountCredentialDto(string Email, string TempPassword);
