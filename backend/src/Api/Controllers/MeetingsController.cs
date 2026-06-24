using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oypa.Crm.Application.Features.Meetings;
using Oypa.Crm.Contracts.Common;
using Oypa.Crm.Contracts.Meetings;

namespace Oypa.Crm.Api.Controllers;

[ApiController]
[Route("api/meetings")]
[Authorize]
public sealed class MeetingsController(IMeetingService meetingService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var data = await meetingService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<MeetingDto>>.Ok(data));
    }

    /// <summary>
    /// Görüşmeleri sayfalama + arama + sıralama ile listeler (Takvim etkilenmez).
    /// sortBy: date | company | status (varsayılan: date desc)
    /// </summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] PagedQuery query,
        CancellationToken cancellationToken)
    {
        var data = await meetingService.GetPagedAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResult<MeetingDto>>.Ok(data));
    }

    [HttpPost]
    public async Task<IActionResult> Schedule(ScheduleMeetingRequest request, CancellationToken cancellationToken)
    {
        var data = await meetingService.ScheduleAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<MeetingDto>.Ok(data, "Görüşme planlandı."));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateMeetingStatusRequest request, CancellationToken cancellationToken)
    {
        await meetingService.UpdateStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponse.Ok("Görüşme durumu güncellendi."));
    }

    [HttpPost("{id:guid}/notes")]
    public async Task<IActionResult> AddNote(Guid id, AddMeetingNoteRequest request, CancellationToken cancellationToken)
    {
        var data = await meetingService.AddNoteAsync(id, request, cancellationToken);
        return Ok(ApiResponse<MeetingDto>.Ok(data, "Not eklendi."));
    }
}
