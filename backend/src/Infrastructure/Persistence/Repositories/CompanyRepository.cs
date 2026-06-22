using Microsoft.EntityFrameworkCore;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Infrastructure.Persistence.Repositories;

/// <summary>
/// Company'ye özgü sorgular. Tüm listeler <c>AsNoTracking</c> ile çalışır;
/// güncelleme gerektiren <see cref="GetByIdWithRepAsync"/> tracking açık döner.
/// </summary>
public sealed class CompanyRepository(AppDbContext db) : Repository<Company>(db), ICompanyRepository
{
    public async Task<IReadOnlyList<Company>> ListLeadsAsync(
        LeadStatus? status,
        CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(c => c.AssignedSalesRep)
            .Where(c => c.Type == CompanyType.Lead && (status == null || c.LeadStatus == status))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Company>> ListCustomersAsync(
        CustomerStatus? status,
        CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(c => c.AssignedSalesRep)
            .Where(c => c.Type == CompanyType.Customer && (status == null || c.CustomerStatus == status))
            .ToListAsync(cancellationToken);

    public async Task<Company?> GetByIdWithRepAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await Set
            .Include(c => c.AssignedSalesRep)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
}
