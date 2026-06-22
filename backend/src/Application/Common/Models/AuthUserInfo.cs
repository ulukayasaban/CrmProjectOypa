namespace Oypa.Crm.Application.Common.Models;

/// <summary>Identity altyapısından dönen, token üretiminde kullanılan kullanıcı bilgisi.</summary>
public sealed record AuthUserInfo(
    Guid Id,
    string Email,
    string FullName,
    string? Position,
    string? Phone,
    IReadOnlyList<string> Roles);
