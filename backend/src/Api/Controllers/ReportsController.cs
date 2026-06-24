using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oypa.Crm.Application.Common.Interfaces;

namespace Oypa.Crm.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public sealed class ReportsController(IReportService reportService) : ControllerBase
{
    /// <summary>
    /// Tüm görüşmeleri (notlar dahil) içeren Excel raporunu indirir.
    /// Ham File döndürülür — ApiResponse zarfı uygulanmaz; FE blob olarak alır.
    /// </summary>
    [HttpGet("meetings")]
    public async Task<IActionResult> GetMeetingReport(CancellationToken cancellationToken)
    {
        var report = await reportService.BuildMeetingReportAsync(cancellationToken);
        return File(report.Content, report.ContentType, report.FileName);
    }

    /// <summary>
    /// Tüm ihaleleri içeren Excel raporunu indirir.
    /// Ham File döndürülür — ApiResponse zarfı uygulanmaz; FE blob olarak alır.
    /// Dosya adı: ihaleler-YYYYMMDD.xlsx
    /// </summary>
    [HttpGet("tenders")]
    public async Task<IActionResult> GetTendersReport(CancellationToken cancellationToken)
    {
        var report = await reportService.BuildTendersReportAsync(cancellationToken);
        return File(report.Content, report.ContentType, report.FileName);
    }
}
