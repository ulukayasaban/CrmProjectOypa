using System.Text;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Features.MailDrafts;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Shouldly;

namespace Oypa.Crm.UnitTests.Features.MailDrafts;

public sealed class MailDraftServiceTests
{
    private readonly IRepository<MailDraft> _mailDrafts = Substitute.For<IRepository<MailDraft>>();
    private readonly IMeetingRepository _meetingRepository = Substitute.For<IMeetingRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<MailDraftService> _logger = Substitute.For<ILogger<MailDraftService>>();

    private MailDraftService CreateSut() => new(_mailDrafts, _meetingRepository, _unitOfWork, _logger);

    private static MailDraft NewDraft(string? cc = null, Guid? meetingId = null) =>
        new("to@x.com", "Konu", "Govde", meetingId, cc);

    // ── SendAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_DraftNotFound_ThrowsNotFound()
    {
        _mailDrafts.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((MailDraft?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(() => sut.SendAsync(Guid.NewGuid()));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_AlreadySent_ThrowsConflict()
    {
        var draft = NewDraft();
        draft.MarkSent();
        _mailDrafts.GetByIdAsync(draft.Id, Arg.Any<CancellationToken>()).Returns(draft);
        var sut = CreateSut();

        await Should.ThrowAsync<ConflictException>(() => sut.SendAsync(draft.Id));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_PendingDraft_MarksSentAndSaves()
    {
        var draft = NewDraft();
        _mailDrafts.GetByIdAsync(draft.Id, Arg.Any<CancellationToken>()).Returns(draft);
        var sut = CreateSut();

        await sut.SendAsync(draft.Id);

        draft.Sent.ShouldBeTrue();
        draft.SentAtUtc.ShouldNotBeNull();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── BuildEmlAsync — sade (Meeting bağlantısı yok) ─────────────────────

    [Fact]
    public async Task BuildEmlAsync_DraftNotFound_ThrowsNotFound()
    {
        _mailDrafts.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((MailDraft?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(() => sut.BuildEmlAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task BuildEmlAsync_DraftWithoutCc_ReturnsValidEml_WithRequiredHeaders_AndNoCcLine()
    {
        var draft = NewDraft(); // Cc = null, MeetingId = null
        _mailDrafts.GetByIdAsync(draft.Id, Arg.Any<CancellationToken>()).Returns(draft);
        var sut = CreateSut();

        var (fileName, content) = await sut.BuildEmlAsync(draft.Id);
        var eml = Encoding.UTF8.GetString(content);

        fileName.ShouldBe($"toplanti-{draft.Id}.eml");
        eml.ShouldContain("To: ");
        eml.ShouldContain("X-Unsent: 1");
        eml.ShouldContain("Content-Transfer-Encoding: base64");
        eml.ShouldContain("=?UTF-8?B?");
        eml.ShouldNotContain("Cc: ");
        // Sade .eml: ICS bölümü olmamalı
        eml.ShouldNotContain("BEGIN:VCALENDAR");
    }

    [Fact]
    public async Task BuildEmlAsync_DraftWithCc_ReturnsEmlWithCcLine()
    {
        var draft = NewDraft(cc: "contact@example.com");
        _mailDrafts.GetByIdAsync(draft.Id, Arg.Any<CancellationToken>()).Returns(draft);
        var sut = CreateSut();

        var (fileName, content) = await sut.BuildEmlAsync(draft.Id);
        var eml = Encoding.UTF8.GetString(content);

        fileName.ShouldBe($"toplanti-{draft.Id}.eml");
        eml.ShouldContain("Cc: contact@example.com");
    }

    // ── BuildEmlAsync — ICS (Meeting bağlantısı var) ──────────────────────

    [Fact]
    public async Task BuildEmlAsync_DraftWithMeetingId_ReturnsMultipartEml_WithIcsPart()
    {
        var meetingId = Guid.NewGuid();
        var draft = NewDraft(meetingId: meetingId);
        _mailDrafts.GetByIdAsync(draft.Id, Arg.Any<CancellationToken>()).Returns(draft);

        // Meeting stub — Company ve Address alanlarıyla
        var meeting = MakeMeeting(meetingId, companyTitle: "Test A.S.", address: "Ankara");
        _meetingRepository.GetByIdWithDetailsAsync(meetingId, Arg.Any<CancellationToken>()).Returns(meeting);

        var sut = CreateSut();

        var (fileName, content) = await sut.BuildEmlAsync(draft.Id);
        var eml = Encoding.UTF8.GetString(content);

        fileName.ShouldBe($"toplanti-{draft.Id}.eml");

        // Multipart yapısı
        eml.ShouldContain("Content-Type: multipart/mixed");
        eml.ShouldContain("----=_OypaCrmIcsBoundary001");

        // ICS kısmı base64 encode edilmiş; base64 decode yapıp içeriği doğrula
        var icsRaw = ExtractIcsPart(eml);
        icsRaw.ShouldContain("BEGIN:VCALENDAR");
        icsRaw.ShouldContain("BEGIN:VEVENT");
        icsRaw.ShouldContain("DTSTART:");
        icsRaw.ShouldContain("METHOD:REQUEST");
        icsRaw.ShouldContain("END:VEVENT");
        icsRaw.ShouldContain("END:VCALENDAR");
    }

    [Fact]
    public async Task BuildEmlAsync_DraftWithMeetingId_IcsContainsSummaryAndLocation()
    {
        var meetingId = Guid.NewGuid();
        var draft = NewDraft(meetingId: meetingId);
        _mailDrafts.GetByIdAsync(draft.Id, Arg.Any<CancellationToken>()).Returns(draft);

        var meeting = MakeMeeting(meetingId, companyTitle: "Ornek Sirket", address: "Istanbul, Besiktas");
        _meetingRepository.GetByIdWithDetailsAsync(meetingId, Arg.Any<CancellationToken>()).Returns(meeting);

        var sut = CreateSut();
        var (_, content) = await sut.BuildEmlAsync(draft.Id);
        var eml = Encoding.UTF8.GetString(content);

        var icsRaw = ExtractIcsPart(eml);
        icsRaw.ShouldContain("SUMMARY:OYPA Görüşme: Ornek Sirket");
        icsRaw.ShouldContain("LOCATION:Istanbul\\, Besiktas");
    }

    [Fact]
    public async Task BuildEmlAsync_DraftWithMeetingId_WhenMeetingNotFoundInDb_StillProducesValidIcs()
    {
        var meetingId = Guid.NewGuid();
        var draft = NewDraft(meetingId: meetingId);
        _mailDrafts.GetByIdAsync(draft.Id, Arg.Any<CancellationToken>()).Returns(draft);

        // Meeting repository null döner (referans tutarlılık sorunu simülasyonu)
        _meetingRepository.GetByIdWithDetailsAsync(meetingId, Arg.Any<CancellationToken>()).Returns((Meeting?)null);

        var sut = CreateSut();
        var (_, content) = await sut.BuildEmlAsync(draft.Id);
        var eml = Encoding.UTF8.GetString(content);

        eml.ShouldContain("Content-Type: multipart/mixed");
        var icsRaw = ExtractIcsPart(eml);
        icsRaw.ShouldContain("BEGIN:VCALENDAR");
        icsRaw.ShouldContain("BEGIN:VEVENT");
    }

    [Fact]
    public async Task BuildEmlAsync_DraftWithMeetingId_AttendeeIsSetToRecipient()
    {
        var meetingId = Guid.NewGuid();
        var draft = NewDraft(cc: "cc@firm.com", meetingId: meetingId);
        _mailDrafts.GetByIdAsync(draft.Id, Arg.Any<CancellationToken>()).Returns(draft);

        var meeting = MakeMeeting(meetingId, companyTitle: "Firma", address: "Adres");
        _meetingRepository.GetByIdWithDetailsAsync(meetingId, Arg.Any<CancellationToken>()).Returns(meeting);

        var sut = CreateSut();
        var (_, content) = await sut.BuildEmlAsync(draft.Id);
        var eml = Encoding.UTF8.GetString(content);

        var icsRaw = ExtractIcsPart(eml);
        icsRaw.ShouldContain("to@x.com");       // To alanı ATTENDEE olarak
        icsRaw.ShouldContain("cc@firm.com");     // Cc alanı ATTENDEE olarak
    }

    // ── GetByMeetingAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByMeetingAsync_MatchingDraft_ReturnsDtoWithCc()
    {
        var meetingId = Guid.NewGuid();
        var draft = new MailDraft("rep@oypa.com", "Konu", "Govde", meetingId, "contact@firm.com");
        _mailDrafts.ListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<MailDraft, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<MailDraft> { draft });
        var sut = CreateSut();

        var result = await sut.GetByMeetingAsync(meetingId);

        result.MeetingId.ShouldBe(meetingId);
        result.To.ShouldBe("rep@oypa.com");
        result.Cc.ShouldBe("contact@firm.com");
    }

    [Fact]
    public async Task GetByMeetingAsync_NoMatchingDraft_ThrowsNotFound()
    {
        _mailDrafts.ListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<MailDraft, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<MailDraft>());
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(() => sut.GetByMeetingAsync(Guid.NewGuid()));
    }

    // ── IcsBuilder doğrudan testler ──────────────────────────────────────────

    [Fact]
    public void IcsBuilder_Build_ContainsRequiredFields()
    {
        var dtStart = new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc);

        var ics = IcsBuilder.Build(
            dtStart: dtStart,
            summary: "Test Toplanti",
            location: "Ankara",
            organizer: "org@test.com",
            attendees: ["att@test.com"],
            uid: "test-uid-123");

        ics.ShouldContain("BEGIN:VCALENDAR");
        ics.ShouldContain("METHOD:REQUEST");
        ics.ShouldContain("BEGIN:VEVENT");
        ics.ShouldContain("DTSTART:20260715T100000Z");
        ics.ShouldContain("DTEND:20260715T110000Z");  // +1 saat varsayılan
        ics.ShouldContain("SUMMARY:Test Toplanti");
        ics.ShouldContain("LOCATION:Ankara");
        ics.ShouldContain("ORGANIZER:mailto:org@test.com");
        ics.ShouldContain("ATTENDEE;RSVP=TRUE:mailto:att@test.com");
        ics.ShouldContain("UID:test-uid-123");
        ics.ShouldContain("END:VEVENT");
        ics.ShouldContain("END:VCALENDAR");
    }

    [Fact]
    public void IcsBuilder_Build_CrlfLineEndings()
    {
        var ics = IcsBuilder.Build(dtStart: DateTime.UtcNow, summary: "Test");
        // RFC 5545: satır sonları CRLF olmalı
        ics.ShouldContain("\r\n");
        ics.ShouldNotContain("\r\r\n");
    }

    [Fact]
    public void IcsBuilder_Build_EscapesCommaInLocation()
    {
        var ics = IcsBuilder.Build(dtStart: DateTime.UtcNow, summary: "S", location: "Istanbul, Kadikoy");
        ics.ShouldContain("LOCATION:Istanbul\\, Kadikoy");
    }

    // ── Yardımcılar ─────────────────────────────────────────────────────────

    /// <summary>
    /// EML içindeki ICS bölümünü base64 decode edip döner.
    /// Multipart boundary ile ICS Content-Type bloğunu ayırır.
    /// </summary>
    private static string ExtractIcsPart(string eml)
    {
        const string calendarMarker = "Content-Type: text/calendar";
        var calIdx = eml.IndexOf(calendarMarker, StringComparison.Ordinal);
        calIdx.ShouldBeGreaterThanOrEqualTo(0, "ICS bölümü bulunamadı");

        // Boş satırdan sonraki base64 bloğunu bul
        var headerEnd = eml.IndexOf("\r\n\r\n", calIdx, StringComparison.Ordinal);
        headerEnd.ShouldBeGreaterThanOrEqualTo(0);
        headerEnd += 4; // "\r\n\r\n" atla

        // Boundary başlangıcı veya end-of-string
        var nextBoundary = eml.IndexOf("\r\n--", headerEnd, StringComparison.Ordinal);
        var b64Block = nextBoundary > 0
            ? eml[headerEnd..nextBoundary]
            : eml[headerEnd..];

        b64Block = b64Block.Trim().Replace("\r\n", "");
        var rawBytes = Convert.FromBase64String(b64Block);
        return Encoding.UTF8.GetString(rawBytes);
    }

    /// <summary>Reflection kullanmadan Meeting oluşturmak için public factory'i taklit eder.</summary>
    private static Meeting MakeMeeting(Guid id, string companyTitle, string address)
    {
        // Meeting.Schedule factory metodunu kullan; Company navigasyonu EF tarafından doldurulur,
        // test ortamında ise doğrudan Company örneği atayamayız (private setter).
        // IcsBuilder.BuildFromMeeting Company?.Title ?? "Firma" kullandığından
        // Company null olduğunda "Firma" yazacak; bu test "Ornek Sirket" bekliyor.
        // Çözüm: yansıma ile Company'yi set etmek yerine IcsBuilder.Build doğrudan test ediliyor.
        // Burada sadece Date/Time/Address/Id'yi doğrulayan testler için stub yeterli.
        var m = Meeting.Schedule(
            companyId: Guid.NewGuid(),
            salesRepId: Guid.NewGuid(),
            contactId: null,
            date: new DateOnly(2026, 8, 10),
            time: new TimeOnly(14, 30),
            address: address,
            method: MeetingMethod.Visit);

        // Id'yi override etmek için reflection (Domain entity private setter)
        var idProp = typeof(Oypa.Crm.Domain.Common.BaseEntity).GetProperty("Id");
        idProp?.SetValue(m, id);

        // Company navigasyonunu set etmek için; Company private constructor'lı olduğundan
        // reflection ile oluştur ve Title'ı ata
        SetCompanyNavigation(m, companyTitle);

        return m;
    }

    private static void SetCompanyNavigation(Meeting meeting, string companyTitle)
    {
        // Company entity'si private constructor'a sahip; reflection ile instance oluştur
        var companyType = typeof(Oypa.Crm.Domain.Entities.Company);
        var company = (Oypa.Crm.Domain.Entities.Company)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(companyType);

        var titleProp = companyType.GetProperty("Title");
        titleProp?.SetValue(company, companyTitle);

        var companyNavProp = typeof(Meeting).GetProperty("Company");
        companyNavProp?.SetValue(meeting, company);
    }
}
