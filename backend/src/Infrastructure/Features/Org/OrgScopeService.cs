using Microsoft.EntityFrameworkCore;
using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Infrastructure.Persistence;

namespace Oypa.Crm.Infrastructure.Features.Org;

/// <summary>
/// Çağıranın org kapsamını çözer ve alıcı kullanıcı kümesi hesaplamalarını yapar.
/// EmployeeService'teki ResolveScopeAsync/GetSubtreeIdsAsync mantığından taşındı ve genişletildi.
/// </summary>
public sealed class OrgScopeService(
    AppDbContext db,
    ICurrentUser currentUser) : IOrgScopeService
{
    public async Task<OrgScope> ResolveAsync(CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            throw new ForbiddenAppException("Bu işlem için giriş yapmanız gerekmektedir.");

        var userId = currentUser.UserId.Value;

        // Çağıranın bağlı Employee düğümünü bul
        var linked = await db.Set<Employee>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ApplicationUserId == userId, cancellationToken);

        if (linked is null)
        {
            // Org'a bağlı değil; Admin rolündeyse tüm personele erişir
            if (currentUser.Roles.Contains("Admin"))
                return new OrgScope(true, []);

            throw new ForbiddenAppException("Personel yönetim kapsamı belirlenemedi.");
        }

        // Kök düğüm (ManagerId == null) → tüm personel
        if (linked.ManagerId is null)
            return new OrgScope(true, []);

        // Alt-ağacı (kendisi dahil) hesapla
        var subtree = await GetSubtreeIdsAsync(linked.Id, cancellationToken);
        if (subtree.Count == 0)
            throw new ForbiddenAppException("Yönetim kapsamınızda personel bulunmuyor.");

        return new OrgScope(false, subtree);
    }

    public async Task<HashSet<Guid>> GetSubtreeIdsAsync(Guid rootId, CancellationToken cancellationToken = default)
    {
        // Tüm çalışanların (id, managerId) çiftini tek sorguda çek; bellekte yürü
        var allPairs = await db.Set<Employee>()
            .AsNoTracking()
            .Select(e => new { e.Id, e.ManagerId })
            .ToListAsync(cancellationToken);

        var childrenMap = allPairs
            .Where(e => e.ManagerId.HasValue)
            .GroupBy(e => e.ManagerId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Id).ToList());

        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(rootId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current))
                continue;

            if (childrenMap.TryGetValue(current, out var children))
                foreach (var child in children)
                    queue.Enqueue(child);
        }

        return visited;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Guid>> GetSubtreeUserIdsAsync(
        Guid unitNodeId,
        CancellationToken cancellationToken = default)
    {
        // Alt-ağaçtaki tüm Employee Id'lerini hesapla (BFS, bellekte)
        var subtreeIds = await GetSubtreeIdsAsync(unitNodeId, cancellationToken);

        // Hesaplı (ApplicationUserId != null) olanların UserId'lerini döndür
        var userIds = await db.Set<Employee>()
            .AsNoTracking()
            .Where(e => subtreeIds.Contains(e.Id) && e.ApplicationUserId != null)
            .Select(e => e.ApplicationUserId!.Value)
            .ToListAsync(cancellationToken);

        return userIds;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Guid>> GetAncestorUserIdsAsync(
        Guid employeeNodeId,
        CancellationToken cancellationToken = default)
    {
        // Tüm Employee (id, managerId, applicationUserId) üçlüsünü tek sorguda çek; bellekte yürü
        var allNodes = await db.Set<Employee>()
            .AsNoTracking()
            .Select(e => new { e.Id, e.ManagerId, e.ApplicationUserId })
            .ToListAsync(cancellationToken);

        var nodeMap = allNodes.ToDictionary(e => e.Id);

        var ancestorUserIds = new List<Guid>();
        var visited = new HashSet<Guid>();

        // Başlangıç düğümünün kendisini atla; yalnızca üst zincire bak
        if (!nodeMap.TryGetValue(employeeNodeId, out var start))
            return ancestorUserIds;

        var current = start.ManagerId;
        while (current.HasValue && !visited.Contains(current.Value))
        {
            visited.Add(current.Value);

            if (!nodeMap.TryGetValue(current.Value, out var node))
                break;

            if (node.ApplicationUserId.HasValue)
                ancestorUserIds.Add(node.ApplicationUserId.Value);

            current = node.ManagerId;
        }

        return ancestorUserIds;
    }

    /// <inheritdoc/>
    public Task<Employee?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.Set<Employee>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ApplicationUserId == userId, cancellationToken);
}
