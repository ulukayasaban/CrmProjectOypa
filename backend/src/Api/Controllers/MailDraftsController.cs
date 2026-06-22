using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oypa.Crm.Application.Features.MailDrafts;
using Oypa.Crm.Contracts.Common;
using Oypa.Crm.Contracts.MailDrafts;

namespace Oypa.Crm.Api.Controllers;

[ApiController]
[Route("api/maildrafts")]
[Authorize]
public sealed class MailDraftsController(IMailDraftService mailDraftService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var data = await mailDraftService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<MailDraftDto>>.Ok(data));
    }

    [HttpPost("{id:guid}/send")]
    public async Task<IActionResult> Send(Guid id, CancellationToken cancellationToken)
    {
        await mailDraftService.SendAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Mail simüle olarak gönderildi."));
    }

    [HttpGet("{id:guid}/eml")]
    public async Task<IActionResult> DownloadEml(Guid id, CancellationToken cancellationToken)
    {
        var (name, bytes) = await mailDraftService.BuildEmlAsync(id, cancellationToken);
        return File(bytes, "message/rfc822", name);
    }

    [HttpGet("by-meeting/{meetingId:guid}")]
    public async Task<IActionResult> GetByMeeting(Guid meetingId, CancellationToken cancellationToken)
    {
        var data = await mailDraftService.GetByMeetingAsync(meetingId, cancellationToken);
        return Ok(ApiResponse<MailDraftDto>.Ok(data));
    }
}
