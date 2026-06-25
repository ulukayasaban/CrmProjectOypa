namespace Oypa.Crm.Application.Common.Interfaces;

/// <summary>Katman-nötr rapor dosyası çıktısı.</summary>
public sealed record ReportFile(byte[] Content, string FileName, string ContentType);

/// <summary>Rapor üretme işlemlerini soyutlar; implementasyon Infrastructure katmanındadır.</summary>
public interface IReportService
{
    /// <summary>
    /// Görüşmeleri (notlar dahil) içeren Excel raporu üretir.
    /// İsteğe bağlı <paramref name="from"/>/<paramref name="to"/> ile görüşme tarihine göre filtrelenir (dahil).
    /// </summary>
    Task<ReportFile> BuildMeetingReportAsync(
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// İhaleleri içeren Excel raporu üretir.
    /// Kolonlar: İhale No, Başlık, Firma, İş Kolu, Tarih, Tahmini Değer, Personel, Hacim, Miktar, Durum, Atanan Temsilci.
    /// İsteğe bağlı <paramref name="from"/>/<paramref name="to"/> ile ihale tarihine göre filtrelenir (dahil).
    /// Dosya adı: ihaleler-YYYYMMDD.xlsx
    /// </summary>
    Task<ReportFile> BuildTendersReportAsync(
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Aktif hedefleri ilerlemeleriyle birlikte içeren Excel raporu üretir.
    /// Kolonlar: Başlık, Segment, Atanan Personel, Haftalık Hedef, Toplam Gerçekleşen, Hafta Sayısı, Durum, Oluşturulma.
    /// </summary>
    Task<ReportFile> BuildGoalReportAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Müşterileri (pipeline) içeren Excel raporu üretir.
    /// Kolonlar: Firma, İş Kolu, Durum, Telefon, E-posta, Şehir, Atanan Temsilci, Oluşturulma.
    /// İsteğe bağlı <paramref name="from"/>/<paramref name="to"/> ile oluşturulma tarihine göre filtrelenir (dahil).
    /// </summary>
    Task<ReportFile> BuildCustomerReportAsync(
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default);
}
