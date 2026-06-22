namespace Oypa.Crm.Contracts.Auth;

public sealed record AuthResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string RefreshToken,
    UserDto User);
