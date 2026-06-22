using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Application.Common.Interfaces;

/// <summary>Çağıranın org kapsamını çözer ve alıcı kullanıcı kümesi hesaplamalarını sağlar.</summary>
public interface IOrgScopeService
{
    /// <summary>
    /// Çağıranın yönetim kapsamını döndürür.
    /// AllEmployees=true → tüm org; AllEmployees=false + Ids → yalnız belirtilen küme.
    /// </summary>
    Task<OrgScope> ResolveAsync(CancellationToken cancellationToken = default);

    /// <summary>BFS ile belirli bir düğümün tüm alt-ağaç Id'lerini döndürür (kendisi dahil).</summary>
    Task<HashSet<Guid>> GetSubtreeIdsAsync(Guid rootId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirtilen org düğümünün alt-ağacındaki <c>ApplicationUserId != null</c> kullanıcıları döndürür.
    /// Manuel bildirim gönderiminde hedef birimi kullanıcılara genişletmek için kullanılır.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetSubtreeUserIdsAsync(Guid unitNodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verilen Employee düğümünün ManagerId zincirini yukarı yürüyerek hesaplı yönetici kullanıcı Id'lerini döndürür.
    /// Otomatik olay bildirimlerinde yönetici zincirine ulaşmak için kullanılır.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetAncestorUserIdsAsync(Guid employeeNodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verilen ApplicationUserId'ye bağlı Employee düğümünü döndürür; yoksa null.
    /// </summary>
    Task<Employee?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>Çağıranın yönetim kapsamı.</summary>
public sealed record OrgScope(bool AllEmployees, HashSet<Guid> Ids);
