namespace Oypa.Crm.Application.Common.Options;

/// <summary>JWT yapılandırması. <c>Secret</c> environment/user-secrets'tan gelir, repoya yazılmaz.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "OypaCrm";
    public string Audience { get; set; } = "OypaCrmClient";
    public string Secret { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
