namespace Oypa.Crm.Application.Common.Interfaces;

/// <summary>Katman-nötr rapor dosyası çıktısı.</summary>
public sealed record ReportFile(byte[] Content, string FileName, string ContentType);

/// <summary>Rapor üretme işlemlerini soyutlar; implementasyon Infrastructure katmanındadır.</summary>
public interface IReportService
{
    /// <summary>Tüm görüşmeleri (notlar dahil) içeren Excel raporu üretir.</summary>
    Task<ReportFile> BuildMeetingReportAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tüm ihaleleri içeren Excel raporu üretir.
    /// Kolonlar: İhale No, Başlık, Firma, İş Kolu, Tarih, Tahmini Değer, Personel, Hacim, Miktar, Durum, Atanan Temsilci.
    /// Dosya adı: ihaleler-YYYYMMDD.xlsx
    /// </summary>
    Task<ReportFile> BuildTendersReportAsync(CancellationToken cancellationToken = default);
}
