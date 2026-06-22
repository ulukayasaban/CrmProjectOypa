using System.Text;
using Microsoft.Extensions.Logging;
using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Mappings;
using Oypa.Crm.Contracts.MailDrafts;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Application.Features.MailDrafts;

public sealed class MailDraftService(
    IRepository<MailDraft> mailDrafts,
    IUnitOfWork unitOfWork,
    ILogger<MailDraftService> logger) : IMailDraftService
{
    public async Task<IReadOnlyList<MailDraftDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await mailDrafts.ListAsync(cancellationToken);
        return list.OrderByDescending(d => d.CreatedAtUtc).Select(d => d.ToDto()).ToList();
    }

    public async Task SendAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var draft = await mailDrafts.GetByIdAsync(id, cancellationToken)
                    ?? throw NotFoundException.For("Mail taslağı", id);

        if (draft.Sent)
            throw new ConflictException("Bu taslak zaten gönderilmiş.");

        draft.MarkSent();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Mail taslağı simüle olarak gönderildi. DraftId={DraftId} To={To}",
            draft.Id, draft.To);
    }

    public async Task<(string FileName, byte[] Content)> BuildEmlAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var draft = await mailDrafts.GetByIdAsync(id, cancellationToken)
                    ?? throw NotFoundException.For("Mail taslağı", id);

        var sb = new StringBuilder();
        sb.Append($"To: {draft.To}\r\n");

        if (!string.IsNullOrEmpty(draft.Cc))
            sb.Append($"Cc: {draft.Cc}\r\n");

        var subjectB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(draft.Subject));
        sb.Append($"Subject: =?UTF-8?B?{subjectB64}?=\r\n");
        sb.Append($"Date: {DateTime.UtcNow:R}\r\n");
        sb.Append("X-Unsent: 1\r\n");
        sb.Append("MIME-Version: 1.0\r\n");
        sb.Append("Content-Type: text/plain; charset=utf-8\r\n");
        sb.Append("Content-Transfer-Encoding: base64\r\n");
        sb.Append("\r\n");

        var bodyB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(draft.Body));
        // RFC2045 satır uzunluğu önerisi: 76 karakter
        for (var i = 0; i < bodyB64.Length; i += 76)
        {
            sb.Append(bodyB64.AsSpan(i, Math.Min(76, bodyB64.Length - i)));
            sb.Append("\r\n");
        }

        var emlBytes = Encoding.UTF8.GetBytes(sb.ToString());
        return ($"toplanti-{id}.eml", emlBytes);
    }

    public async Task<MailDraftDto> GetByMeetingAsync(Guid meetingId, CancellationToken cancellationToken = default)
    {
        var list = await mailDrafts.ListAsync(d => d.MeetingId == meetingId, cancellationToken);
        var draft = list.OrderByDescending(d => d.CreatedAtUtc).FirstOrDefault()
                    ?? throw NotFoundException.For("Görüşmeye ait mail taslağı", meetingId);

        return draft.ToDto();
    }
}
