using System.Linq.Expressions;
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
    private readonly ITenderRepository _tenderRepository = Substitute.For<ITenderRepository>();
    private readonly IRepository<Goal> _goalRepository = Substitute.For<IRepository<Goal>>();
    private readonly IRepository<GoalWeek> _goalWeekRepository = Substitute.For<IRepository<GoalWeek>>();
    private readonly IRepository<Employee> _employeeRepository = Substitute.For<IRepository<Employee>>();
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();

    private ExcelReportService CreateSut() => new(
        _meetingRepository,
        _tenderRepository,
        _goalRepository,
        _goalWeekRepository,
        _employeeRepository,
        _companyRepository);

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

    // -----------------------------------------------------------------------
    // Tarih aralığı filtresi
    // -----------------------------------------------------------------------

    [Fact]
    public async Task BuildMeetingReportAsync_DateRange_ExcludesOutOfRange()
    {
        var inRange = MakeMeetingOn("Range A.Ş.", new DateOnly(2026, 6, 15));
        var outRange = MakeMeetingOn("Out A.Ş.", new DateOnly(2026, 7, 20));
        _meetingRepository.ListWithDetailsAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { inRange, outRange });

        var sut = CreateSut();
        var report = await sut.BuildMeetingReportAsync(
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30));

        using var ms = new MemoryStream(report.Content);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First();

        // Yalnızca aralıktaki görüşme yazılmalı (row 2); row 3 boş.
        ws.Cell(2, 1).GetString().ShouldBe("Range A.Ş.");
        ws.Cell(3, 1).GetString().ShouldBeEmpty();
    }

    // -----------------------------------------------------------------------
    // Hedef raporu
    // -----------------------------------------------------------------------

    [Fact]
    public async Task BuildGoalReportAsync_EmptyList_ReturnsXlsxWithHeaders()
    {
        SetupEmptyGoalData();

        var sut = CreateSut();
        var report = await sut.BuildGoalReportAsync();

        report.Content.ShouldNotBeEmpty();
        report.FileName.ShouldStartWith("hedefler-");

        using var ms = new MemoryStream(report.Content);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First();
        ws.Cell(1, 1).GetString().ShouldBe("Başlık");
        ws.Cell(1, 5).GetString().ShouldBe("Toplam Gerçekleşen");
    }

    [Fact]
    public async Task BuildGoalReportAsync_OneGoal_WritesAssigneeAndProgress()
    {
        var employee = new Employee("Satış Müdürü", "Avniye ÖNER");
        var goal = new Goal(employee.Id, GoalSegment.Customer, 5, "Q3 Hedefi");

        var week1 = new GoalWeek(goal.Id, new DateOnly(2026, 6, 1), 5);
        week1.SetAchieved(3);
        var week2 = new GoalWeek(goal.Id, new DateOnly(2026, 6, 8), 5);
        week2.SetAchieved(4);

        _goalRepository.ListAsync(Arg.Any<Expression<Func<Goal, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { goal });
        _goalWeekRepository.ListAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { week1, week2 });
        _employeeRepository.ListAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { employee });
        _companyRepository.ListCustomersAsync(Arg.Any<Domain.Enums.CustomerStatus?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Company>());

        var sut = CreateSut();
        var report = await sut.BuildGoalReportAsync();

        using var ms = new MemoryStream(report.Content);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First();

        ws.Cell(2, 1).GetString().ShouldBe("Q3 Hedefi");
        ws.Cell(2, 3).GetString().ShouldBe("Avniye ÖNER");
        ws.Cell(2, 4).GetDouble().ShouldBe(5);
        ws.Cell(2, 5).GetDouble().ShouldBe(7);  // 3 + 4
        ws.Cell(2, 6).GetDouble().ShouldBe(2);  // 2 hafta
    }

    // -----------------------------------------------------------------------
    // Müşteri raporu
    // -----------------------------------------------------------------------

    [Fact]
    public async Task BuildCustomerReportAsync_OneCustomer_WritesRow()
    {
        var customer = new Company("Müşteri A.Ş.", Sector.Energy, "555", "m@a.c", "Adres");
        _companyRepository.ListCustomersAsync(Arg.Any<Domain.Enums.CustomerStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new[] { customer });

        var sut = CreateSut();
        var report = await sut.BuildCustomerReportAsync();

        report.FileName.ShouldStartWith("musteriler-");

        using var ms = new MemoryStream(report.Content);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First();
        ws.Cell(1, 1).GetString().ShouldBe("Firma");
        ws.Cell(2, 1).GetString().ShouldBe("Müşteri A.Ş.");
        ws.Cell(2, 2).GetString().ShouldBe("Enerji");
    }

    [Fact]
    public async Task BuildCustomerReportAsync_DateRange_ExcludesOutOfRange()
    {
        var inRange = new Company("İçeride", Sector.Retail, "1", "a@b.c", "Adr")
        { CreatedAtUtc = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc) };
        var outRange = new Company("Dışarıda", Sector.Retail, "2", "c@d.e", "Adr")
        { CreatedAtUtc = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc) };

        _companyRepository.ListCustomersAsync(Arg.Any<Domain.Enums.CustomerStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new[] { inRange, outRange });

        var sut = CreateSut();
        var report = await sut.BuildCustomerReportAsync(
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30));

        using var ms = new MemoryStream(report.Content);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First();

        // Sıralama Title'a göre; yalnız "İçeride" olmalı.
        ws.Cell(2, 1).GetString().ShouldBe("İçeride");
        ws.Cell(3, 1).GetString().ShouldBeEmpty();
    }

    // -----------------------------------------------------------------------
    // Yardımcılar
    // -----------------------------------------------------------------------

    private static Meeting MakeMeetingOn(string companyTitle, DateOnly date)
    {
        var company = new Company(companyTitle, Sector.Retail, "1", "a@b.c", "Adr");
        var rep = new SalesRep("Temsilci", "temsilci@oypa.com");
        var meeting = Meeting.Schedule(
            company.Id, rep.Id, null, date, new TimeOnly(10, 0), "Adres", MeetingMethod.Visit);
        SetNavigation(meeting, "Company", company);
        SetNavigation(meeting, "SalesRep", rep);
        return meeting;
    }

    private void SetupEmptyGoalData()
    {
        _goalRepository.ListAsync(Arg.Any<Expression<Func<Goal, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Goal>());
        _goalWeekRepository.ListAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<GoalWeek>());
        _employeeRepository.ListAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Employee>());
    }
}
