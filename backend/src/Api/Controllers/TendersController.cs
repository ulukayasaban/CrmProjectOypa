using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Oypa.Crm.Api.Extensions;
using Oypa.Crm.Application.Features.Tenders;
using Oypa.Crm.Contracts.Common;
using Oypa.Crm.Contracts.Tenders;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Api.Controllers;

[ApiController]
[Route("api/tenders")]
[Authorize]
public sealed class TendersController(ITenderService tenderService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Sector? sector,
        [FromQuery] TenderStatus? status,
        CancellationToken cancellationToken)
    {
        var data = await tenderService.GetAsync(sector, status, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TenderDto>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var data = await tenderService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<TenderDto>.Ok(data));
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitingExtensions.AdminSensitive)]
    public async Task<IActionResult> Create(CreateTenderRequest request, CancellationToken cancellationToken)
    {
        var data = await tenderService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<TenderDto>.Ok(data, "İhale oluşturuldu."));
    }

    [HttpPut("{id:guid}")]
    [EnableRateLimiting(RateLimitingExtensions.AdminSensitive)]
    public async Task<IActionResult> Update(Guid id, UpdateTenderRequest request, CancellationToken cancellationToken)
    {
        var data = await tenderService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<TenderDto>.Ok(data, "İhale güncellendi."));
    }

    [HttpPatch("{id:guid}/status")]
    [EnableRateLimiting(RateLimitingExtensions.AdminSensitive)]
    public async Task<IActionResult> ChangeStatus(Guid id, ChangeTenderStatusRequest request, CancellationToken cancellationToken)
    {
        await tenderService.ChangeStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponse.Ok("Durum güncellendi."));
    }

    [HttpDelete("{id:guid}")]
    [EnableRateLimiting(RateLimitingExtensions.AdminSensitive)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await tenderService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("İhale silindi."));
    }
}
