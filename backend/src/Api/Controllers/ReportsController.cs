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
    /// Görüşmeleri (notlar dahil) içeren Excel raporunu indirir.
    /// İsteğe bağlı <paramref name="from"/>/<paramref name="to"/> (yyyy-MM-dd) ile tarih aralığı filtrelenir.
    /// Ham File döndürülür — ApiResponse zarfı uygulanmaz; FE blob olarak alır.
    /// </summary>
    [HttpGet("meetings")]
    public async Task<IActionResult> GetMeetingReport(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var report = await reportService.BuildMeetingReportAsync(from, to, cancellationToken);
        return File(report.Content, report.ContentType, report.FileName);
    }

    /// <summary>
    /// İhaleleri içeren Excel raporunu indirir.
    /// İsteğe bağlı <paramref name="from"/>/<paramref name="to"/> (yyyy-MM-dd) ile ihale tarihine göre filtrelenir.
    /// Dosya adı: ihaleler-YYYYMMDD.xlsx
    /// </summary>
    [HttpGet("tenders")]
    public async Task<IActionResult> GetTendersReport(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var report = await reportService.BuildTendersReportAsync(from, to, cancellationToken);
        return File(report.Content, report.ContentType, report.FileName);
    }

    /// <summary>
    /// Aktif hedefleri ilerlemeleriyle içeren Excel raporunu indirir.
    /// Dosya adı: hedefler-YYYYMMDD.xlsx
    /// </summary>
    [HttpGet("goals")]
    public async Task<IActionResult> GetGoalReport(CancellationToken cancellationToken)
    {
        var report = await reportService.BuildGoalReportAsync(cancellationToken);
        return File(report.Content, report.ContentType, report.FileName);
    }

    /// <summary>
    /// Müşterileri (pipeline) içeren Excel raporunu indirir.
    /// İsteğe bağlı <paramref name="from"/>/<paramref name="to"/> (yyyy-MM-dd) ile oluşturulma tarihine göre filtrelenir.
    /// Dosya adı: musteriler-YYYYMMDD.xlsx
    /// </summary>
    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomerReport(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var report = await reportService.BuildCustomerReportAsync(from, to, cancellationToken);
        return File(report.Content, report.ContentType, report.FileName);
    }
}
