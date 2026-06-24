using ClosedXML.Excel;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Infrastructure.Reports;

/// <summary>
/// ClosedXML kullanarak görüşme ve ihale Excel raporları üretir.
/// ClosedXML bağımlılığı yalnızca bu katmanda (Infrastructure) bulunur — Clean Architecture kuralı.
/// </summary>
public sealed class ExcelReportService(
    IMeetingRepository meetingRepository,
    ITenderRepository tenderRepository) : IReportService
{
    private const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public async Task<ReportFile> BuildMeetingReportAsync(CancellationToken cancellationToken = default)
    {
        var meetingList = await meetingRepository.ListWithDetailsAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Görüşmeler");

        // Başlık satırı
        var headers = new[]
        {
            "Firma", "İlgili Kişi", "Temsilci", "Ünvan",
            "Tarih", "Saat", "Yöntem", "Durum", "Durum Notu", "Notlar"
        };

        ApplyHeaderStyle(sheet, headers);

        // Veri satırları
        int row = 2;
        foreach (var meeting in meetingList)
        {
            // Notları "yyyy-MM-dd HH:mm YazarAdı: İçerik" formatında birleştir.
            var notesText = string.Join(
                "\n",
                meeting.Notes
                    .OrderBy(n => n.CreatedAtUtc)
                    .Select(n => $"{n.CreatedAtUtc:yyyy-MM-dd HH:mm} {n.AuthorName}: {n.Content}"));

            sheet.Cell(row, 1).Value = meeting.Company?.Title ?? string.Empty;
            sheet.Cell(row, 2).Value = meeting.Contact?.Name ?? string.Empty;
            sheet.Cell(row, 3).Value = meeting.SalesRep?.Name ?? string.Empty;
            sheet.Cell(row, 4).Value = meeting.SalesRep?.Employee?.Title ?? string.Empty;
            sheet.Cell(row, 5).Value = meeting.Date.ToString("yyyy-MM-dd");
            sheet.Cell(row, 6).Value = meeting.Time.ToString("HH:mm");
            sheet.Cell(row, 7).Value = MeetingMethodLabel(meeting.Method.ToString());
            sheet.Cell(row, 8).Value = MeetingStatusLabel(meeting.Status.ToString());
            sheet.Cell(row, 9).Value = meeting.Comment ?? string.Empty;

            var notesCell = sheet.Cell(row, 10);
            notesCell.Value = notesText;
            if (!string.IsNullOrEmpty(notesText))
                notesCell.Style.Alignment.WrapText = true;

            row++;
        }

        // Sütun genişliklerini otomatik ayarla.
        sheet.Columns().AdjustToContents();

        // Notlar sütununu sabit genişliğe sabitle (çok uzun metinler için).
        sheet.Column(10).Width = 60;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ReportFile(stream.ToArray(), "Gorusme-Raporu.xlsx", ContentType);
    }

    public async Task<ReportFile> BuildTendersReportAsync(CancellationToken cancellationToken = default)
    {
        // Tüm ihaleleri Company ve AssignedSalesRep ilişkileriyle getir.
        var tenders = await tenderRepository.ListAsync(sector: null, status: null, cancellationToken);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("İhaleler");

        // Başlık satırı — spec'teki sütun sırası korunur.
        var headers = new[]
        {
            "İhale No", "Başlık", "Firma", "İş Kolu", "Tarih",
            "Tahmini Değer", "Personel", "Hacim", "Miktar", "Durum", "Atanan Temsilci"
        };

        ApplyHeaderStyle(sheet, headers);

        // Veri satırları
        int row = 2;
        foreach (var tender in tenders)
        {
            sheet.Cell(row, 1).Value = tender.TenderNumber ?? string.Empty;
            sheet.Cell(row, 2).Value = tender.Title;
            sheet.Cell(row, 3).Value = tender.Company?.Title ?? string.Empty;
            sheet.Cell(row, 4).Value = SectorLabel(tender.Sector);
            sheet.Cell(row, 5).Value = tender.TenderDate.ToString("yyyy-MM-dd");

            // Sayısal alanlar: null ise boş bırak; değer varsa sayı olarak yaz.
            if (tender.EstimatedValue.HasValue)
                sheet.Cell(row, 6).Value = (double)tender.EstimatedValue.Value;
            if (tender.PersonnelCount.HasValue)
                sheet.Cell(row, 7).Value = tender.PersonnelCount.Value;
            if (tender.Volume.HasValue)
                sheet.Cell(row, 8).Value = (double)tender.Volume.Value;
            if (tender.Quantity.HasValue)
                sheet.Cell(row, 9).Value = tender.Quantity.Value;

            sheet.Cell(row, 10).Value = TenderStatusLabel(tender.Status);
            sheet.Cell(row, 11).Value = tender.AssignedSalesRep?.Name ?? string.Empty;

            row++;
        }

        sheet.Columns().AdjustToContents();

        // Tarih ve başlık sütunları çok uzun olmamalı
        sheet.Column(2).Width = Math.Min(sheet.Column(2).Width, 50);
        sheet.Column(3).Width = Math.Min(sheet.Column(3).Width, 50);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        // Dosya adı: ihaleler-YYYYMMDD.xlsx
        var fileName = $"ihaleler-{DateTime.UtcNow:yyyyMMdd}.xlsx";
        return new ReportFile(stream.ToArray(), fileName, ContentType);
    }

    // ─── Yardımcılar ───────────────────────────────────────────────────────────

    /// <summary>Başlık satırını stillendirir; mavi arka plan, beyaz kalın yazı.</summary>
    private static void ApplyHeaderStyle(IXLWorksheet sheet, string[] headers)
    {
        for (int col = 0; col < headers.Length; col++)
        {
            var cell = sheet.Cell(1, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            cell.Style.Font.FontColor = XLColor.White;
        }
    }

    private static string MeetingMethodLabel(string method) => method switch
    {
        "Visit" => "Yüz Yüze Ziyaret",
        "Phone" => "Telefon Görüşmesi",
        "Email" => "E-mail / Teklif",
        _ => method
    };

    private static string MeetingStatusLabel(string status) => status switch
    {
        "Planned" => "Planlandı",
        "Done" => "Yapıldı",
        "Cancelled" => "İptal",
        _ => status
    };

    private static string TenderStatusLabel(TenderStatus status) => status switch
    {
        TenderStatus.Hazirlik      => "Hazırlık",
        TenderStatus.TeklifVerildi => "Teklif Verildi",
        TenderStatus.Kazanildi     => "Kazanıldı",
        TenderStatus.Kaybedildi    => "Kaybedildi",
        TenderStatus.Iptal         => "İptal",
        _ => status.ToString()
    };

    private static string SectorLabel(Sector sector) => sector switch
    {
        Sector.Tourism           => "Turizm",
        Sector.Retail            => "Perakende",
        Sector.FacilityManagement => "Tesis Yönetimi",
        Sector.Energy            => "Enerji",
        Sector.Other             => "Diğer",
        _ => sector.ToString()
    };
}
