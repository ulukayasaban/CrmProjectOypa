using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Mappings;
using Oypa.Crm.Contracts.Categories;
using Oypa.Crm.Contracts.Companies;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Application.Features.Categories;

public sealed class CategoryService(
    IRepository<Category> categories,
    IUnitOfWork unitOfWork) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await categories.ListAsync(cancellationToken);
        return list.Select(c => c.ToDto()).ToList();
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = new Category(request.Name, request.Color);
        await categories.AddAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return category.ToDto();
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await categories.GetByIdAsync(id, cancellationToken)
                       ?? throw NotFoundException.For("Kategori", id);

        category.Rename(request.Name);
        category.SetColor(request.Color);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return category.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await categories.GetByIdAsync(id, cancellationToken)
                       ?? throw NotFoundException.For("Kategori", id);

        category.MarkDeleted(DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Bu metot <see cref="ICompanyService.SetCategoriesAsync"/> üzerinden çağrılır;
    /// <see cref="ICategoryService"/> arayüzünde de yer alır ancak iş mantığı CompanyService'de tutulur.
    /// Bu implementasyon doğrudan kullanılmak istenirse yönlendirme sağlar.
    /// </summary>
    public Task<CompanyDto> SetCompanyCategoriesAsync(
        Guid companyId,
        IReadOnlyList<Guid> categoryIds,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException(
            "Firma kategori ataması CompanyService.SetCategoriesAsync üzerinden yapılmalıdır.");
}
