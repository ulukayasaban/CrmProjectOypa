using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Oypa.Crm.Api.Hubs;

/// <summary>
/// Gerçek zamanlı bildirim push'u için SignalR hub'ı.
/// Yalnızca kimliği doğrulanmış kullanıcılar bağlanabilir.
/// İstemci tarafı olay adı: "ReceiveNotification".
/// </summary>
[Authorize]
public sealed class NotificationsHub : Hub
{
    // Hub bu versiyonda yalnızca sunucu→istemci push için kullanılır;
    // istemciden gelen mesaj işleme metodu tanımlı değil.
    // İleride istemci-tetiklemeli işlemler buraya eklenebilir.
}
