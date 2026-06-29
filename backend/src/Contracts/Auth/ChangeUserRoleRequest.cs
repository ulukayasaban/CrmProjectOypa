namespace Oypa.Crm.Contracts.Auth;

/// <summary>Admin tarafından başka bir kullanıcının rolünü değiştirme isteği.</summary>
public sealed record ChangeUserRoleRequest(string Role);
