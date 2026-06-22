using System.IdentityModel.Tokens.Jwt;
using Oypa.Crm.Application.Common.Interfaces;

namespace Oypa.Crm.Api.Common;

/// <summary>JWT claim'lerinden geçerli kullanıcı bilgisini okur.</summary>
public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUser
{
    private System.Security.Claims.ClaimsPrincipal? User => accessor.HttpContext?.User;

    public Guid? UserId =>
        Guid.TryParse(User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : null;

    public string? Email => User?.FindFirst(JwtRegisteredClaimNames.Email)?.Value;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyList<string> Roles =>
        User?.FindAll("role").Select(c => c.Value).ToList() ?? [];
}
