using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.SignalR;

namespace Oypa.Crm.Api.Hubs;

/// <summary>
/// SignalR kullanıcı kimliğini JWT <c>sub</c> claim'inden türetir.
/// Bu sayede <c>Clients.User(userId)</c> ile kullanıcıya özel push mümkün olur.
/// </summary>
public sealed class NotificationHubUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
}
