namespace Oypa.Crm.Application.Common.Models;

/// <summary>Üretilen erişim token'ı ve geçerlilik bitişi.</summary>
public sealed record TokenResult(string Token, DateTime ExpiresAtUtc);
