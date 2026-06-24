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

    public async Task<(IReadOnlyList<Tender> Items, int TotalCount)> ListPagedAsync(
        Sector? sector,
        TenderStatus? status,
        IReadOnlyCollection<TenderStatus>? statuses,
        string? search,
        string? sortBy,
        bool descending,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Temel sorgu — mevcut include'lar korunur
        var query = Set.AsNoTracking()
            .Include(t => t.Company)
            .Include(t => t.AssignedSalesRep)
            .AsQueryable();

        // Mevcut filtreler
        if (sector.HasValue)
            query = query.Where(t => t.Sector == sector.Value);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        // Çoklu durum filtresi (segment: örn. Aktif = Hazirlik + TeklifVerildi).
        // Filtre sunucuda uygulanır ki sayfalama ve toplam sayı segment'e göre doğru olsun.
        if (statuses is { Count: > 0 })
            query = query.Where(t => statuses.Contains(t.Status));

        // Serbest metin araması: başlık, ihale numarası veya firma adı
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(t =>
                t.Title.ToLower().Contains(term) ||
                (t.TenderNumber != null && t.TenderNumber.ToLower().Contains(term)) ||
                (t.Company != null && t.Company.Title.ToLower().Contains(term)));
        }

        // Toplam kayıt sayısı — sayfa kesmesinden önce hesaplanır
        var totalCount = await query.CountAsync(cancellationToken);

        // Sıralama — bilinmeyen alan varsayılana (TenderDate desc) düşer
        query = (sortBy?.ToLower()) switch
        {
            "title"          => descending ? query.OrderByDescending(t => t.Title)          : query.OrderBy(t => t.Title),
            "company"        => descending ? query.OrderByDescending(t => t.Company!.Title) : query.OrderBy(t => t.Company!.Title),
            "estimatedvalue" => descending ? query.OrderByDescending(t => t.EstimatedValue)  : query.OrderBy(t => t.EstimatedValue),
            "status"         => descending ? query.OrderByDescending(t => t.Status)          : query.OrderBy(t => t.Status),
            // Varsayılan: tenderDate desc
            _                => query.OrderByDescending(t => t.TenderDate)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
