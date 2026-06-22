namespace Oypa.Crm.Application.Common.Interfaces;

/// <summary>Geçerli HTTP isteğindeki kimliği doğrulanmış kullanıcı bilgisi.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<string> Roles { get; }
}
