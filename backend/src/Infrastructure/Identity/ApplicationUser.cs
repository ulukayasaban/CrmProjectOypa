using Microsoft.AspNetCore.Identity;

namespace Oypa.Crm.Infrastructure.Identity;

/// <summary>ASP.NET Identity kullanıcı modelinin OYPA alanlarıyla genişletilmiş hali.</summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public string? Position { get; set; }
}
