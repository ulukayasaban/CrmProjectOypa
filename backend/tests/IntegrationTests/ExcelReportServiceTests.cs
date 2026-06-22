using ClosedXML.Excel;
using NSubstitute;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Oypa.Crm.Infrastructure.Reports;
using Shouldly;

namespace Oypa.Crm.IntegrationTests;

/// <summary>
/// ExcelReportService birim testleri — IMeetingRepository NSubstitute ile mock'lanır;
/// ClosedXML çıktısı doğrudan üzerinde açılarak doğrulanır.
/// </summary>
public sealed class ExcelReportServiceTests
{
    private readonly IMeetingRepository _meetingRepository = Substitute.For<IMeetingRepository>();

    private ExcelReportService CreateSut() => new(_meetingRepository);

    private static Meeting MakeMeeting(string companyTitle, string repName, string? repTitle = null)
    {
        var company = new Company(companyTitle, Sector.Retail, "1", "a@b.c", "Adr");
        var rep = new SalesRep(repName, $"{repName.Replace(" ", "").ToLower()}@oypa.com");

        var meeting = Meeting.Schedule(
            company.Id, rep.Id, null,
            new DateOnly(2026, 6, 11), new TimeOnly(10, 0), "Test Adres", MeetingMethod.Visit);

        // Navigation property'leri reflection ile ata — EF Core'un yaptığını simüle eder.
        SetNavigation(meeting, "Company", company);
        SetNavigation(meeting, "SalesRep", rep);

        if (repTitle is not null)
        {
            var emp = new Employee(repTitle, repName);
            SetNavigation(rep, "Employee", emp);
        }

        return meeting;
    }

    private static void SetNavigation<T>(object entity, string propName, T value)
    {
        var prop = entity.GetType().GetProperty(propName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);
        prop?.SetValue(entity, value);
    }

    // -----------------------------------------------------------------------
    // Temel çıktı doğrulamaları
    // -----------------------------------------------------------------------

    [Fact]
    public async Task BuildMeetingReportAsync_EmptyList_ReturnsNonEmptyBytes()
    {
        _meetingRepository.ListWithDetailsAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Meeting>());

        var sut = CreateSut();
        var report = await sut.BuildMeetingReportAsync();

        report.Content.ShouldNotBeEmpty();
        report.Content.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task BuildMeetingReportAsync_ReturnsXlsxContentType()
    {
        _meetingRepository.ListWithDetailsAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Meeting>());

        var sut = CreateSut();
        var report = await sut.BuildMeetingReportAsync();

        report.ContentType.ShouldBe("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Fact]
    public async Task BuildMeetingReportAsync_BytesStartWithXlsxZipSignature()
    {
        // xlsx dosyaları ZIP formatındadır; ilk 4 byte PK\x03\x04 olmalı.
        _meetingRepository.ListWithDetailsAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Meeting>());

        var sut = CreateSut();
        var report = await sut.BuildMeetingReportAsync();

        report.Content[0].ShouldBe((byte)0x50, "Byte 0 'P' olmalı");
        report.Content[1].ShouldBe((byte)0x4B, "Byte 1 'K' olmalı");
        report.Content[2].ShouldBe((byte)0x03, "Byte 2 0x03 olmalı");
        report.Content[3].ShouldBe((byte)0x04, "Byte 3 0x04 olmalı");
    }

    [Fact]
    public async Task BuildMeetingReportAsync_FileNameIsCorrect()
    {
        _meetingRepository.ListWithDetailsAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Meeting>());

        var sut = CreateSut();
        var report = await sut.BuildMeetingReportAsync();

        report.FileName.ShouldBe("Gorusme-Raporu.xlsx");
    }

    // -----------------------------------------------------------------------
    // Veri satırı — başlık satırı ve içerik
    // -----------------------------------------------------------------------

    [Fact]
    public async Task BuildMeetingReportAsync_OneRow_WorkbookHasDataRow()
    {
        var meeting = MakeMeeting("Acme A.Ş.", "Halil KÜTÜKCÜ");
        _meetingRepository.ListWithDetailsAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { meeting });

        var sut = CreateSut();
        var report = await sut.BuildMeetingReportAsync();

        // Çıktıyı tekrar açıp veri satırını doğrula.
        using var ms = new MemoryStream(report.Content);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First();

        // Başlık satırı (row 1) + 1 veri satırı (row 2) olmalı.
        ws.Cell(2, 1).GetString().ShouldBe("Acme A.Ş.");
        ws.Cell(2, 3).GetString().ShouldBe("Halil KÜTÜKCÜ");
    }

    [Fact]
    public async Task BuildMeetingReportAsync_HeaderRow_HasExpectedColumnTitles()
    {
        _meetingRepository.ListWithDetailsAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Meeting>());

        var sut = CreateSut();
        var report = await sut.BuildMeetingReportAsync();

        using var ms = new MemoryStream(report.Content);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First();

        ws.Cell(1, 1).GetString().ShouldBe("Firma");
        ws.Cell(1, 3).GetString().ShouldBe("Temsilci");
        ws.Cell(1, 10).GetString().ShouldBe("Notlar");
    }

    // -----------------------------------------------------------------------
    // Notlar kolonu — içerik doğrulaması
    // -----------------------------------------------------------------------

    [Fact]
    public async Task BuildMeetingReportAsync_MeetingWithNotes_NotesColumnContainsNoteContent()
    {
        var meeting = MakeMeeting("Beta Ltd.", "Avniye ÖNER", "Satış Müdürü");
        meeting.AddNote("İlk not içeriği", Guid.NewGuid(), "Avniye ÖNER", "Satış Müdürü");
        meeting.AddNote("İkinci not", Guid.NewGuid(), "Muhammed MARANGOZ", "Satış Uzmanı");

        _meetingRepository.ListWithDetailsAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { meeting });

        var sut = CreateSut();
        var report = await sut.BuildMeetingReportAsync();

        using var ms = new MemoryStream(report.Content);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First();

        var notesCell = ws.Cell(2, 10).GetString();
        notesCell.ShouldContain("İlk not içeriği");
        notesCell.ShouldContain("İkinci not");
        notesCell.ShouldContain("Avniye ÖNER");
        notesCell.ShouldContain("Muhammed MARANGOZ");
    }

    [Fact]
    public async Task BuildMeetingReportAsync_MeetingWithNotes_NotesAreSortedByCreatedAtUtc()
    {
        var meeting = MakeMeeting("Gamma Inc.", "Test Temsilci");

        // Notları ekle; AddNote void döndürür.
        meeting.AddNote("A notu", null, "Yazar1", null);
        meeting.AddNote("B notu", null, "Yazar2", null);

        _meetingRepository.ListWithDetailsAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { meeting });

        var sut = CreateSut();
        var report = await sut.BuildMeetingReportAsync();

        using var ms = new MemoryStream(report.Content);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First();

        var notesText = ws.Cell(2, 10).GetString();
        // "A notu" "B notu"'ndan önce gelmeli.
        notesText.IndexOf("A notu", StringComparison.Ordinal)
            .ShouldBeLessThan(notesText.IndexOf("B notu", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BuildMeetingReportAsync_MeetingWithNoNotes_NotesColumnIsEmpty()
    {
        var meeting = MakeMeeting("Delta S.A.", "Temsilci");
        _meetingRepository.ListWithDetailsAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { meeting });

        var sut = CreateSut();
        var report = await sut.BuildMeetingReportAsync();

        using var ms = new MemoryStream(report.Content);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First();

        ws.Cell(2, 10).GetString().ShouldBeEmpty();
    }
}
