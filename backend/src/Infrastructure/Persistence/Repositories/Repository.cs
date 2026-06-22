using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Domain.Common;

namespace Oypa.Crm.Infrastructure.Persistence.Repositories;

/// <summary>EF Core tabanlı genel depo. Okumalar varsayılan olarak <c>AsNoTracking</c>.</summary>
public class Repository<T>(AppDbContext db) : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext Db = db;
    protected DbSet<T> Set => Db.Set<T>();

    // FindAsync izlenen (tracked) varlık döner — güncelleme senaryoları için uygundur.
    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Set.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<T>> ListAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    public async Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default) =>
        predicate is null
            ? await Set.CountAsync(cancellationToken)
            : await Set.CountAsync(predicate, cancellationToken);

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await Set.AddAsync(entity, cancellationToken);

    public void Update(T entity) => Set.Update(entity);

    public void Remove(T entity) => Set.Remove(entity);
}
