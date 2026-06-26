using System.Text;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Application.Features.MailDrafts;

/// <summary>
/// RFC 5545 uyumlu ICS (iCalendar) VCALENDAR/VEVENT bloğu üretir.
/// Bu yardımcı saf (pure) bir statik sınıftır; bağımlılık gerektirmez ve doğrudan test edilebilir.
/// METHOD:REQUEST kullanıldığından alıcı Outlook/macOS Takvim ile daveti takvime ekleyebilir.
/// Teams join link için Microsoft Graph kimlik bilgisi gerekir; bu sınıf yalnızca standart ICS üretir.
/// </summary>
public static class IcsBuilder
{
    /// <summary>
    /// Tek bir VEVENT içeren VCALENDAR dizgesi döner.
    /// </summary>
    /// <param name="dtStart">Toplantı başlangıç tarihi ve saati (UTC).</param>
    /// <param name="dtEnd">Toplantı bitiş tarihi ve saati (UTC). Null ise dtStart + 1 saat kullanılır.</param>
    /// <param name="summary">SUMMARY alanı (takvimde görünecek başlık).</param>
    /// <param name="location">LOCATION alanı (toplantı adresi). Null ise eklenmez.</param>
    /// <param name="description">DESCRIPTION alanı (gövde metni). Null ise eklenmez.</param>
    /// <param name="organizer">Organizatör e-posta adresi (ORGANIZER:mailto:...).</param>
    /// <param name="attendees">Katılımcı e-posta adresleri (her biri ATTENDEE satırı alır).</param>
    /// <param name="uid">Etkinlik UID'si (deduplication için; boş geçilirse Guid.NewGuid() üretilir).</param>
    /// <param name="dtstamp">DTSTAMP (üretim zamanı, UTC). Null ise DateTime.UtcNow kullanılır.</param>
    public static string Build(
        DateTime dtStart,
        DateTime? dtEnd = null,
        string summary = "",
        string? location = null,
        string? description = null,
        string organizer = "noreply@oypa.com.tr",
        IEnumerable<string>? attendees = null,
        string? uid = null,
        DateTime? dtstamp = null)
    {
        var effectiveDtEnd = dtEnd ?? dtStart.AddHours(1);
        var effectiveUid = string.IsNullOrWhiteSpace(uid) ? Guid.NewGuid().ToString() : uid;
        var effectiveDtStamp = dtstamp ?? DateTime.UtcNow;

        var sb = new StringBuilder();

        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//OYPA CRM//TR");
        sb.AppendLine("CALSCALE:GREGORIAN");
        sb.AppendLine("METHOD:REQUEST");

        sb.AppendLine("BEGIN:VEVENT");
        sb.AppendLine($"UID:{effectiveUid}");
        sb.AppendLine($"DTSTAMP:{FormatUtc(effectiveDtStamp)}");
        sb.AppendLine($"DTSTART:{FormatUtc(dtStart)}");
        sb.AppendLine($"DTEND:{FormatUtc(effectiveDtEnd)}");
        sb.AppendLine(FoldProperty("SUMMARY", EscapeText(summary)));

        if (!string.IsNullOrWhiteSpace(location))
            sb.AppendLine(FoldProperty("LOCATION", EscapeText(location)));

        if (!string.IsNullOrWhiteSpace(description))
            sb.AppendLine(FoldProperty("DESCRIPTION", EscapeText(description)));

        sb.AppendLine($"ORGANIZER:mailto:{organizer}");

        foreach (var attendee in attendees ?? [])
        {
            if (!string.IsNullOrWhiteSpace(attendee))
                sb.AppendLine($"ATTENDEE;RSVP=TRUE:mailto:{attendee}");
        }

        sb.AppendLine("STATUS:CONFIRMED");
        sb.AppendLine("END:VEVENT");
        sb.AppendLine("END:VCALENDAR");

        // ICS satır sonları RFC 5545 gereği CRLF olmalı; StringBuilder.AppendLine OS'a bağımlıdır.
        return sb.ToString().Replace("\r\n", "\n").Replace("\n", "\r\n");
    }

    /// <summary>
    /// Meeting entity'sinden ICS dizgesi türetir.
    /// Görüşme tarihi + saatini UTC DateTime'a çevirir (DateOnly + TimeOnly → DateTime).
    /// </summary>
    public static string BuildFromMeeting(
        Meeting meeting,
        string organizerEmail = "noreply@oypa.com.tr",
        IEnumerable<string>? attendees = null)
    {
        // DateOnly + TimeOnly → UTC DateTime (yerel saat bilgisi olmadığından UTC sayılır;
        // ileride TimeZoneInfo.ConvertTimeToUtc ile iyileştirilebilir).
        var dtStart = new DateTime(
            meeting.Date.Year, meeting.Date.Month, meeting.Date.Day,
            meeting.Time.Hour, meeting.Time.Minute, meeting.Time.Second,
            DateTimeKind.Utc);

        var companyTitle = meeting.Company?.Title ?? "Firma";
        var summary = $"OYPA Görüşme: {companyTitle}";
        var location = string.IsNullOrWhiteSpace(meeting.Address) ? null : meeting.Address;
        var uid = $"{meeting.Id}@oypa.crm";

        return Build(
            dtStart: dtStart,
            dtEnd: dtStart.AddHours(1),
            summary: summary,
            location: location,
            organizer: organizerEmail,
            attendees: attendees,
            uid: uid);
    }

    // ─── RFC 5545 yardımcıları ───────────────────────────────────────────────

    /// <summary>DateTime'ı ICS UTC formatına çevirir: YYYYMMDDTHHMMSSZ</summary>
    private static string FormatUtc(DateTime dt) =>
        dt.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'");

    /// <summary>ICS değer dizgelerinde özel karakterleri kaçırır.</summary>
    private static string EscapeText(string text) =>
        text
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\r\n", "\\n")
            .Replace("\r", "\\n")
            .Replace("\n", "\\n");

    /// <summary>
    /// RFC 5545 §3.1: "PROPNAME:value" satırını 75 oktet sınırına göre katlar.
    /// Devam satırları CRLF + SPACE ile başlar.
    /// </summary>
    private static string FoldProperty(string name, string value)
    {
        const int maxOctets = 75;
        var full = $"{name}:{value}";

        if (full.Length <= maxOctets)
            return full;

        var sb = new StringBuilder();
        var remaining = full.AsSpan();

        sb.Append(remaining[..maxOctets]);
        remaining = remaining[maxOctets..];

        while (!remaining.IsEmpty)
        {
            // Devam satırı: 1 boşluk karakteri + en fazla 74 karakter
            const int continuationMax = 74;
            sb.Append("\r\n ");
            var chunk = remaining.Length > continuationMax ? remaining[..continuationMax] : remaining;
            sb.Append(chunk);
            remaining = remaining[chunk.Length..];
        }

        return sb.ToString();
    }
}
