using Microsoft.EntityFrameworkCore;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Infrastructure.Persistence.Repositories;

public sealed class TenderRepository(AppDbContext db) : Repository<Tender>(db), ITenderRepository
{
    public async Task<IReadOnlyList<Tender>> ListAsync(
        Sector? sector,
        TenderStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking()
            .Include(t => t.Company)
            .Include(t => t.AssignedSalesRep)
            .AsQueryable();

        if (sector.HasValue)
            query = query.Where(t => t.Sector == sector.Value);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        return await query
            .OrderByDescending(t => t.TenderDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Tender?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(t => t.Company)
            .Include(t => t.AssignedSalesRep)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Tender>> ListApproachingAsync(
        DateOnly today,
        int daysAhead,
        CancellationToken cancellationToken = default)
    {
        var deadline = today.AddDays(daysAhead);

        return await Set
            .Include(t => t.Company)
            .Include(t => t.AssignedSalesRep)
                .ThenInclude(r => r!.Employee)
            .Where(t =>
                (t.Status == TenderStatus.Hazirlik || t.Status == TenderStatus.TeklifVerildi) &&
                t.ApproachNotifiedAtUtc == null &&
                t.TenderDate >= today &&
                t.TenderDate <= deadline &&
                t.AssignedSalesRepId != null)
            .ToListAsync(cancellationToken);
    }
}
