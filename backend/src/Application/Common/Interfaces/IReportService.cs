namespace Oypa.Crm.Application.Common.Interfaces;

/// <summary>Katman-nötr rapor dosyası çıktısı.</summary>
public sealed record ReportFile(byte[] Content, string FileName, string ContentType);

/// <summary>Rapor üretme işlemlerini soyutlar; implementasyon Infrastructure katmanındadır.</summary>
public interface IReportService
{
    /// <summary>Tüm görüşmeleri (notlar dahil) içeren Excel raporu üretir.</summary>
    Task<ReportFile> BuildMeetingReportAsync(CancellationToken cancellationToken = default);
}
