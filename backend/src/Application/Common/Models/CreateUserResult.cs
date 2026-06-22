namespace Oypa.Crm.Application.Common.Models;

public sealed record CreateUserResult(
    bool Succeeded,
    Guid? UserId,
    IReadOnlyList<string> Errors);
