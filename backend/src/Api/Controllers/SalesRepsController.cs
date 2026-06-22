using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oypa.Crm.Api.Extensions;
using Oypa.Crm.Application.Features.SalesReps;
using Oypa.Crm.Contracts.Common;
using Oypa.Crm.Contracts.SalesReps;

namespace Oypa.Crm.Api.Controllers;

[ApiController]
[Route("api/salesreps")]
[Authorize]
public sealed class SalesRepsController(ISalesRepService salesRepService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var data = await salesRepService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SalesRepDto>>.Ok(data));
    }

    [HttpPost]
    [Authorize(AuthenticationExtensions.AdminPolicy)]
    public async Task<IActionResult> Create(CreateSalesRepRequest request, CancellationToken cancellationToken)
    {
        var data = await salesRepService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<SalesRepDto>.Ok(data, "Temsilci eklendi."));
    }

    [HttpPatch("{id:guid}/employee")]
    [Authorize(AuthenticationExtensions.AdminPolicy)]
    public async Task<IActionResult> LinkEmployee(Guid id, LinkEmployeeRequest request, CancellationToken cancellationToken)
    {
        var data = await salesRepService.LinkEmployeeAsync(id, request.EmployeeId, cancellationToken);
        return Ok(ApiResponse<SalesRepDto>.Ok(data, "Temsilci personele bağlandı."));
    }
}
