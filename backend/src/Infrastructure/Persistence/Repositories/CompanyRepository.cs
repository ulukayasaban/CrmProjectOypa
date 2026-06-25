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
            .Include(c => c.Categories)
            .Where(c => c.Type == CompanyType.Lead && (status == null || c.LeadStatus == status))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Company>> ListCustomersAsync(
        CustomerStatus? status,
        CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(c => c.AssignedSalesRep)
            .Include(c => c.Categories)
            .Where(c => c.Type == CompanyType.Customer && (status == null || c.CustomerStatus == status))
            .ToListAsync(cancellationToken);

    public async Task<Company?> GetByIdWithRepAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await Set
            .Include(c => c.AssignedSalesRep)
            .Include(c => c.Categories)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Company?> GetByIdWithCategoriesAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await Set
            .Include(c => c.AssignedSalesRep)
            .Include(c => c.Categories)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Company> Items, int TotalCount)> ListLeadsPagedAsync(
        LeadStatus? status,
        string? search,
        string? sortBy,
        bool descending,
        int page,
        int pageSize,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildCompanyQuery(CompanyType.Lead, search)
            .Where(c => status == null || c.LeadStatus == status);

        if (categoryId.HasValue)
            query = query.Where(c => c.Categories.Any(cat => cat.Id == categoryId.Value));

        var totalCount = await query.CountAsync(cancellationToken);

        query = ApplyCompanySort(query, sortBy, descending);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<Company> Items, int TotalCount)> ListCustomersPagedAsync(
        CustomerStatus? status,
        string? search,
        string? sortBy,
        bool descending,
        int page,
        int pageSize,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildCompanyQuery(CompanyType.Customer, search)
            .Where(c => status == null || c.CustomerStatus == status);

        if (categoryId.HasValue)
            query = query.Where(c => c.Categories.Any(cat => cat.Id == categoryId.Value));

        var totalCount = await query.CountAsync(cancellationToken);

        query = ApplyCompanySort(query, sortBy, descending);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    // -----------------------------------------------------------------------
    // Özel yardımcı metotlar
    // -----------------------------------------------------------------------

    /// <summary>
    /// Temel firma sorgusunu hazırlar: type filtresi, AssignedSalesRep include ve
    /// serbest metin araması (title, phone, email veya temsilci adı).
    /// </summary>
    private IQueryable<Company> BuildCompanyQuery(CompanyType type, string? search)
    {
        var query = Set.AsNoTracking()
            .Include(c => c.AssignedSalesRep)
                .ThenInclude(r => r!.Employee)
            .Include(c => c.Categories)
            .Where(c => c.Type == type)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.Title.ToLower().Contains(term) ||
                c.Phone.ToLower().Contains(term) ||
                c.Email.ToLower().Contains(term) ||
                (c.AssignedSalesRep != null && c.AssignedSalesRep.Name.ToLower().Contains(term)));
        }

        return query;
    }

    /// <summary>Firma listesine sıralama uygular; bilinmeyen alan createdAt desc'e düşer.</summary>
    private static IQueryable<Company> ApplyCompanySort(
        IQueryable<Company> query,
        string? sortBy,
        bool descending) =>
        (sortBy?.ToLower()) switch
        {
            "title"     => descending ? query.OrderByDescending(c => c.Title)     : query.OrderBy(c => c.Title),
            "city"      => descending ? query.OrderByDescending(c => c.City)      : query.OrderBy(c => c.City),
            "createdat" => descending ? query.OrderByDescending(c => c.CreatedAtUtc) : query.OrderBy(c => c.CreatedAtUtc),
            // Varsayılan: createdAt desc
            _           => query.OrderByDescending(c => c.CreatedAtUtc)
        };
}
