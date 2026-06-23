using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Oypa.Crm.Api.Extensions;
using Oypa.Crm.Application.Features.Goals;
using Oypa.Crm.Contracts.Common;
using Oypa.Crm.Contracts.Goals;

namespace Oypa.Crm.Api.Controllers;

[ApiController]
[Route("api/goals")]
[Authorize]
public sealed class GoalsController(IGoalService goalService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetScoped(CancellationToken cancellationToken)
    {
        var data = await goalService.GetScopedAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<GoalDto>>.Ok(data));
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitingExtensions.AdminSensitive)]
    public async Task<IActionResult> Create(CreateGoalRequest request, CancellationToken cancellationToken)
    {
        var data = await goalService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<GoalDto>.Ok(data, "Hedef oluşturuldu."));
    }

    [HttpPut("{id:guid}")]
    [EnableRateLimiting(RateLimitingExtensions.AdminSensitive)]
    public async Task<IActionResult> Update(Guid id, UpdateGoalRequest request, CancellationToken cancellationToken)
    {
        var data = await goalService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<GoalDto>.Ok(data, "Hedef güncellendi."));
    }

    [HttpDelete("{id:guid}")]
    [EnableRateLimiting(RateLimitingExtensions.AdminSensitive)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await goalService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Hedef silindi."));
    }

    [HttpGet("{id:guid}/weeks")]
    public async Task<IActionResult> GetWeeks(Guid id, CancellationToken cancellationToken)
    {
        var data = await goalService.GetWeeksAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<GoalWeekDto>>.Ok(data));
    }
}
