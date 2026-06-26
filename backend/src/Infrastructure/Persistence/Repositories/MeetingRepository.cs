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

    public async Task<Meeting?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(m => m.Company)
            .Include(m => m.SalesRep)
            .Include(m => m.Contact)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Meeting> Items, int TotalCount)> ListPagedAsync(
        string? search,
        string? sortBy,
        bool descending,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Temel sorgu — mevcut include'lar korunur
        var query = Set.AsNoTracking()
            .Include(m => m.Company)
            .Include(m => m.SalesRep)
                .ThenInclude(r => r!.Employee)
            .Include(m => m.Contact)
            .Include(m => m.Notes)
            .AsQueryable();

        // Serbest metin araması: firma adı veya temsilci adı
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(m =>
                (m.Company != null && m.Company.Title.ToLower().Contains(term)) ||
                (m.SalesRep != null && m.SalesRep.Name.ToLower().Contains(term)));
        }

        // Toplam kayıt sayısı — sayfa kesmesinden önce hesaplanır
        var totalCount = await query.CountAsync(cancellationToken);

        // Sıralama — bilinmeyen alan varsayılana (date desc) düşer
        query = (sortBy?.ToLower()) switch
        {
            "company" => descending ? query.OrderByDescending(m => m.Company!.Title) : query.OrderBy(m => m.Company!.Title),
            "status"  => descending ? query.OrderByDescending(m => m.Status)         : query.OrderBy(m => m.Status),
            // Varsayılan: date desc, time desc
            _         => query.OrderByDescending(m => m.Date).ThenByDescending(m => m.Time)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
