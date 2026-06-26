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
    IMeetingRepository meetingRepository,
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

    /// <summary>
    /// RFC 822 / RFC 2045 uyumlu .eml dosyası üretir.
    /// Taslak bir Meeting'e bağlıysa (MeetingId != null) MIME multipart/mixed yapısı kullanılır
    /// ve text/calendar (ICS METHOD:REQUEST) eki eklenir — böylece alıcı takvime ekleyebilir.
    /// Taslak Meeting'e bağlı değilse sade text/plain .eml üretilir (önceki davranış).
    /// </summary>
    public async Task<(string FileName, byte[] Content)> BuildEmlAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var draft = await mailDrafts.GetByIdAsync(id, cancellationToken)
                    ?? throw NotFoundException.For("Mail taslağı", id);

        byte[] emlBytes;

        if (draft.MeetingId.HasValue)
        {
            var meeting = await meetingRepository.GetByIdWithDetailsAsync(draft.MeetingId.Value, cancellationToken);
            emlBytes = BuildMultipartEml(draft, meeting);
        }
        else
        {
            emlBytes = BuildPlainEml(draft);
        }

        return ($"toplanti-{id}.eml", emlBytes);
    }

    public async Task<MailDraftDto> GetByMeetingAsync(Guid meetingId, CancellationToken cancellationToken = default)
    {
        var list = await mailDrafts.ListAsync(d => d.MeetingId == meetingId, cancellationToken);
        var draft = list.OrderByDescending(d => d.CreatedAtUtc).FirstOrDefault()
                    ?? throw NotFoundException.For("Görüşmeye ait mail taslağı", meetingId);

        return draft.ToDto();
    }

    // ─── EML üretici yardımcılar ────────────────────────────────────────────

    /// <summary>
    /// Meeting'e bağlı taslak için multipart/mixed MIME .eml üretir.
    /// İki bölüm: (1) text/plain gövde, (2) text/calendar ICS daveti.
    /// </summary>
    private static byte[] BuildMultipartEml(MailDraft draft, Meeting? meeting)
    {
        const string boundary = "----=_OypaCrmIcsBoundary001";

        var sb = new StringBuilder();

        // ── RFC 822 / 2822 başlıkları ──────────────────────────────────────
        AppendCommonHeaders(sb, draft);
        sb.Append($"MIME-Version: 1.0\r\n");
        sb.Append($"Content-Type: multipart/mixed; boundary=\"{boundary}\"\r\n");
        sb.Append("\r\n");

        // ── Part 1: text/plain gövde ─────────────────────────────────────
        sb.Append($"--{boundary}\r\n");
        sb.Append("Content-Type: text/plain; charset=utf-8\r\n");
        sb.Append("Content-Transfer-Encoding: base64\r\n");
        sb.Append("\r\n");
        AppendBase64Lines(sb, draft.Body);
        sb.Append("\r\n");

        // ── Part 2: text/calendar (ICS METHOD:REQUEST) ──────────────────
        var attendees = new List<string> { draft.To };
        if (!string.IsNullOrWhiteSpace(draft.Cc))
            attendees.Add(draft.Cc);

        string icsText;
        if (meeting is not null)
        {
            icsText = IcsBuilder.BuildFromMeeting(meeting, attendees: attendees);
        }
        else
        {
            // Meeting DB'de bulunamadı (referans tutarlılık sorunu); en azından UID üretilir.
            icsText = IcsBuilder.Build(
                dtStart: DateTime.UtcNow.AddDays(1),
                summary: "OYPA Görüşme",
                uid: $"{draft.MeetingId}@oypa.crm",
                attendees: attendees);
        }

        sb.Append($"--{boundary}\r\n");
        sb.Append("Content-Type: text/calendar; charset=utf-8; method=REQUEST\r\n");
        sb.Append("Content-Disposition: attachment; filename=\"invite.ics\"\r\n");
        sb.Append("Content-Transfer-Encoding: base64\r\n");
        sb.Append("\r\n");
        AppendBase64Lines(sb, icsText);
        sb.Append("\r\n");

        sb.Append($"--{boundary}--\r\n");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    /// <summary>Meeting bağlantısı olmayan taslak için sade text/plain .eml üretir.</summary>
    private static byte[] BuildPlainEml(MailDraft draft)
    {
        var sb = new StringBuilder();
        AppendCommonHeaders(sb, draft);
        sb.Append("MIME-Version: 1.0\r\n");
        sb.Append("Content-Type: text/plain; charset=utf-8\r\n");
        sb.Append("Content-Transfer-Encoding: base64\r\n");
        sb.Append("\r\n");
        AppendBase64Lines(sb, draft.Body);
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static void AppendCommonHeaders(StringBuilder sb, MailDraft draft)
    {
        sb.Append($"To: {draft.To}\r\n");

        if (!string.IsNullOrEmpty(draft.Cc))
            sb.Append($"Cc: {draft.Cc}\r\n");

        var subjectB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(draft.Subject));
        sb.Append($"Subject: =?UTF-8?B?{subjectB64}?=\r\n");
        sb.Append($"Date: {DateTime.UtcNow:R}\r\n");
        sb.Append("X-Unsent: 1\r\n");
    }

    /// <summary>Verilen metni RFC 2045 uyumlu 76-karakter satırlara böler (base64 olarak).</summary>
    private static void AppendBase64Lines(StringBuilder sb, string text)
    {
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        for (var i = 0; i < b64.Length; i += 76)
        {
            sb.Append(b64.AsSpan(i, Math.Min(76, b64.Length - i)));
            sb.Append("\r\n");
        }
    }
}
