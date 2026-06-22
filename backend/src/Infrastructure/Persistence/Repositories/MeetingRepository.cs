using Microsoft.EntityFrameworkCore;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Infrastructure.Persistence.Repositories;

public sealed class MeetingRepository(AppDbContext db) : Repository<Meeting>(db), IMeetingRepository
{
    public async Task<IReadOnlyList<Meeting>> ListWithDetailsAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(m => m.Company)
            .Include(m => m.SalesRep)
                .ThenInclude(r => r!.Employee)
            .Include(m => m.Contact)
            .Include(m => m.Notes)
            .OrderByDescending(m => m.Date)
            .ThenByDescending(m => m.Time)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Meeting>> ListByCompanyWithDetailsAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(m => m.Company)
            .Include(m => m.SalesRep)
                .ThenInclude(r => r!.Employee)
            .Include(m => m.Contact)
            .Include(m => m.Notes)
            .Where(m => m.CompanyId == companyId)
            .OrderByDescending(m => m.Date)
            .ThenByDescending(m => m.Time)
            .ToListAsync(cancellationToken);

    public async Task<int> CountDoneByRepsAndSegmentAsync(
        IReadOnlyCollection<Guid> salesRepIds,
        DateOnly weekStart,
        DateOnly weekEnd,
        GoalSegment segment,
        CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking()
            .Include(m => m.Company)
            .Where(m =>
                salesRepIds.Contains(m.SalesRepId) &&
                m.Status == MeetingStatus.Done &&
                m.Date >= weekStart &&
                m.Date <= weekEnd &&
                m.Company != null);

        query = segment switch
        {
            GoalSegment.Customer => query.Where(m => m.Company!.Type == CompanyType.Customer),
            GoalSegment.Lead => query.Where(m => m.Company!.Type == CompanyType.Lead),
            GoalSegment.All => query,
            _ => query
        };

        return await query.CountAsync(cancellationToken);
    }
}
