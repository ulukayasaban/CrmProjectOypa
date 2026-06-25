using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oypa.Crm.Api.Extensions;
using Oypa.Crm.Application.Features.Categories;
using Oypa.Crm.Contracts.Categories;
using Oypa.Crm.Contracts.Common;

namespace Oypa.Crm.Api.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public sealed class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    /// <summary>Tüm aktif kategorileri listeler.</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var data = await categoryService.ListAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CategoryDto>>.Ok(data));
    }

    /// <summary>Yeni kategori oluşturur. Yalnızca Admin.</summary>
    [HttpPost]
    [Authorize(AuthenticationExtensions.AdminPolicy)]
    public async Task<IActionResult> Create(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var data = await categoryService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<CategoryDto>.Ok(data, "Kategori oluşturuldu."));
    }

    /// <summary>Kategori adı ve rengini günceller. Yalnızca Admin.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(AuthenticationExtensions.AdminPolicy)]
    public async Task<IActionResult> Update(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var data = await categoryService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<CategoryDto>.Ok(data, "Kategori güncellendi."));
    }

    /// <summary>Kategoriyi soft-delete ile siler. Yalnızca Admin.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationExtensions.AdminPolicy)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await categoryService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Kategori silindi."));
    }
}
