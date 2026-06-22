using ClosedXML.Excel;
using Oypa.Crm.Application.Common.Interfaces;

namespace Oypa.Crm.Infrastructure.Reports;

/// <summary>
/// ClosedXML kullanarak görüşme Excel raporu üretir.
/// ClosedXML bağımlılığı yalnızca bu katmanda (Infrastructure) bulunur — Clean Architecture kuralı.
/// </summary>
public sealed class ExcelReportService(IMeetingRepository meetingRepository) : IReportService
{
    private const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string FileName = "Gorusme-Raporu.xlsx";

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

        for (int col = 0; col < headers.Length; col++)
        {
            var cell = sheet.Cell(1, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            cell.Style.Font.FontColor = XLColor.White;
        }

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

        return new ReportFile(stream.ToArray(), FileName, ContentType);
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
}
