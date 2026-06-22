using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oypa.Crm.Application.Features.Dashboard;
using Oypa.Crm.Contracts.Common;
using Oypa.Crm.Contracts.Dashboard;

namespace Oypa.Crm.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var data = await dashboardService.GetAsync(cancellationToken);
        return Ok(ApiResponse<DashboardDto>.Ok(data));
    }
}
