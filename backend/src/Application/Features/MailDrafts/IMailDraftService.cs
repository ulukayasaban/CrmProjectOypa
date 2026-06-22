using Oypa.Crm.Contracts.MailDrafts;

namespace Oypa.Crm.Application.Features.MailDrafts;

public interface IMailDraftService
{
    Task<IReadOnlyList<MailDraftDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Taslağı "gönderildi" olarak işaretler (simülasyon).</summary>
    Task SendAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>RFC822 .eml dosyası üretir; taslak yoksa NotFoundException fırlatır.</summary>
    Task<(string FileName, byte[] Content)> BuildEmlAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Belirtilen görüşmeye ait en güncel taslağı döner; yoksa NotFoundException.</summary>
    Task<MailDraftDto> GetByMeetingAsync(Guid meetingId, CancellationToken cancellationToken = default);
}
