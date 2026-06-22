namespace Oypa.Crm.Contracts.Auth;

public sealed record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string? Position,
    string? Phone,
    IReadOnlyList<string> Roles);
