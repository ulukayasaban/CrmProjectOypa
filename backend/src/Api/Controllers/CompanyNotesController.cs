using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oypa.Crm.Application.Features.Companies;
using Oypa.Crm.Contracts.Common;
using Oypa.Crm.Contracts.Companies;

namespace Oypa.Crm.Api.Controllers;

[ApiController]
[Route("api/companies/{companyId:guid}/notes")]
[Authorize]
public sealed class CompanyNotesController(ICompanyNoteService companyNoteService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetNotes(Guid companyId, CancellationToken cancellationToken)
    {
        var data = await companyNoteService.GetByCompanyAsync(companyId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CompanyNoteDto>>.Ok(data));
    }

    [HttpPost]
    public async Task<IActionResult> AddNote(Guid companyId, CreateCompanyNoteRequest request, CancellationToken cancellationToken)
    {
        var data = await companyNoteService.AddAsync(companyId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<CompanyNoteDto>.Ok(data, "Not eklendi."));
    }
}
