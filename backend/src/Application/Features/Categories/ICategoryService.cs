using Oypa.Crm.Contracts.Categories;
using Oypa.Crm.Contracts.Companies;

namespace Oypa.Crm.Application.Features.Categories;

public interface ICategoryService
{
    /// <summary>Tüm aktif kategorileri listeler.</summary>
    Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Yeni kategori oluşturur.</summary>
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>Kategori adı ve rengini günceller.</summary>
    Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>Kategoriyi soft-delete ile siler.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Firmaya atanacak kategorileri toptan ayarlar ve güncel <see cref="CompanyDto"/> döner.
    /// </summary>
    Task<CompanyDto> SetCompanyCategoriesAsync(Guid companyId, IReadOnlyList<Guid> categoryIds, CancellationToken cancellationToken = default);
}
