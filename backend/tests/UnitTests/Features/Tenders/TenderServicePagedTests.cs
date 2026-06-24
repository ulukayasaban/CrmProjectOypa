using NSubstitute;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Features.Tenders;
using Oypa.Crm.Contracts.Common;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Shouldly;

namespace Oypa.Crm.UnitTests.Features.Tenders;

/// <summary>
/// TenderService.GetPagedAsync için birim testleri.
/// Sayfa kesimi, arama, sıralama ve toplam kayıt sayısı doğrulanır.
/// </summary>
public sealed class TenderServicePagedTests
{
    private readonly ITenderRepository _tenders = Substitute.For<ITenderRepository>();
    private readonly IRepository<Company> _companies = Substitute.For<IRepository<Company>>();
    private readonly IRepository<SalesRep> _salesReps = Substitute.For<IRepository<SalesRep>>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private TenderService CreateSut() =>
        new(_tenders, _companies, _salesReps, _clock, _unitOfWork);

    private static Company NewCompany() =>
        new("Acme", Sector.Retail, "111", "acme@test.com", "Adres");

    private static Tender NewTender(Guid companyId, string title = "Test İhalesi") =>
        Tender.Create(companyId, title, "IH-001", Sector.Retail,
            new DateOnly(2026, 12, 1), null, 1000m, 500m, null, null, null);

    // -----------------------------------------------------------------------
    // Sayfa kesimi — Items ve TotalCount doğrulaması
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetPagedAsync_ReturnsPagedResult_WithCorrectMetadata()
    {
        var companyId = Guid.NewGuid();
        var tenderList = new[] { NewTender(companyId, "Birinci"), NewTender(companyId, "İkinci") };
        const int totalCount = 10;

        _tenders.ListPagedAsync(
                Arg.Any<Sector?>(), Arg.Any<TenderStatus?>(),
                Arg.Any<IReadOnlyCollection<TenderStatus>?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((tenderList, totalCount));

        var query = new PagedQuery { Page = 2, PageSize = 2 };
        var sut = CreateSut();

        var result = await sut.GetPagedAsync(null, null, null, query);

        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(2);
        result.Page.ShouldBe(2);
        result.PageSize.ShouldBe(2);
        result.TotalCount.ShouldBe(totalCount);
        result.TotalPages.ShouldBe(5); // Math.Ceiling(10 / 2)
    }

    [Fact]
    public async Task GetPagedAsync_EmptyResult_TotalCountZero()
    {
        _tenders.ListPagedAsync(
                Arg.Any<Sector?>(), Arg.Any<TenderStatus?>(),
                Arg.Any<IReadOnlyCollection<TenderStatus>?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Tender>(), 0));

        var query = new PagedQuery { Page = 1, PageSize = 20 };
        var sut = CreateSut();

        var result = await sut.GetPagedAsync(null, null, null, query);

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.TotalPages.ShouldBe(0);
    }

    // -----------------------------------------------------------------------
    // Arama parametresi repository'e iletilir
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetPagedAsync_WithSearch_PassesSearchToRepository()
    {
        _tenders.ListPagedAsync(
                Arg.Any<Sector?>(), Arg.Any<TenderStatus?>(),
                Arg.Any<IReadOnlyCollection<TenderStatus>?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Tender>(), 0));

        var query = new PagedQuery { Search = "Acme" };
        var sut = CreateSut();

        await sut.GetPagedAsync(null, null, null, query);

        await _tenders.Received(1).ListPagedAsync(
            null, null, Arg.Any<IReadOnlyCollection<TenderStatus>?>(), "Acme", Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // Sıralama parametresi repository'e iletilir
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetPagedAsync_WithSortBy_PassesSortToRepository()
    {
        _tenders.ListPagedAsync(
                Arg.Any<Sector?>(), Arg.Any<TenderStatus?>(),
                Arg.Any<IReadOnlyCollection<TenderStatus>?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Tender>(), 0));

        var query = new PagedQuery { SortBy = "title", SortDir = "asc" };
        var sut = CreateSut();

        await sut.GetPagedAsync(null, null, null, query);

        await _tenders.Received(1).ListPagedAsync(
            null, null, Arg.Any<IReadOnlyCollection<TenderStatus>?>(), Arg.Any<string?>(), "title", false,
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPagedAsync_SortDirDesc_PassesDescendingTrue()
    {
        _tenders.ListPagedAsync(
                Arg.Any<Sector?>(), Arg.Any<TenderStatus?>(),
                Arg.Any<IReadOnlyCollection<TenderStatus>?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Tender>(), 0));

        var query = new PagedQuery { SortBy = "estimatedValue", SortDir = "desc" };
        var sut = CreateSut();

        await sut.GetPagedAsync(null, null, null, query);

        await _tenders.Received(1).ListPagedAsync(
            null, null, Arg.Any<IReadOnlyCollection<TenderStatus>?>(), Arg.Any<string?>(), "estimatedValue", true,
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // Mevcut sektör/durum filtreleri paged uca da taşınır
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetPagedAsync_WithSectorAndStatus_PassesFiltersToRepository()
    {
        _tenders.ListPagedAsync(
                Arg.Any<Sector?>(), Arg.Any<TenderStatus?>(),
                Arg.Any<IReadOnlyCollection<TenderStatus>?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Tender>(), 0));

        var query = new PagedQuery();
        var sut = CreateSut();

        await sut.GetPagedAsync(Sector.Energy, TenderStatus.Hazirlik, null, query);

        await _tenders.Received(1).ListPagedAsync(
            Sector.Energy, TenderStatus.Hazirlik,
            Arg.Any<IReadOnlyCollection<TenderStatus>?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // PagedQuery normalize — geçersiz Page/PageSize sıkıştırılır
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0, 1)]   // 0 → 1'e normalize
    [InlineData(-5, 1)]  // negatif → 1'e normalize
    [InlineData(3, 3)]   // geçerli değer korunur
    public void PagedQuery_Page_NormalizedToMinimumOne(int input, int expected)
    {
        var query = new PagedQuery { Page = input };
        query.Page.ShouldBe(expected);
    }

    [Theory]
    [InlineData(0, 1)]    // 0 → 1'e normalize
    [InlineData(200, 100)] // üst sınır 100
    [InlineData(50, 50)]  // geçerli değer korunur
    public void PagedQuery_PageSize_NormalizedToRange(int input, int expected)
    {
        var query = new PagedQuery { PageSize = input };
        query.PageSize.ShouldBe(expected);
    }

    [Theory]
    [InlineData("desc", true)]
    [InlineData("DESC", true)]
    [InlineData("asc", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void PagedQuery_IsDescending_CorrectlyDetected(string? sortDir, bool expectedDesc)
    {
        var query = new PagedQuery { SortDir = sortDir };
        query.IsDescending.ShouldBe(expectedDesc);
    }

    // -----------------------------------------------------------------------
    // Items DTO'ya dönüştürülür
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetPagedAsync_MapsEntitiesToDtos()
    {
        var companyId = Guid.NewGuid();
        var tender = NewTender(companyId, "Benzersiz Başlık");
        _tenders.ListPagedAsync(
                Arg.Any<Sector?>(), Arg.Any<TenderStatus?>(),
                Arg.Any<IReadOnlyCollection<TenderStatus>?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((new[] { tender }, 1));

        var sut = CreateSut();

        var result = await sut.GetPagedAsync(null, null, null, new PagedQuery());

        result.Items.Count.ShouldBe(1);
        result.Items[0].Title.ShouldBe("Benzersiz Başlık");
        result.Items[0].CompanyId.ShouldBe(companyId);
    }

    // -----------------------------------------------------------------------
    // TotalPages hesabı köşe vakaları
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(10, 3, 4)]   // Math.Ceiling(10/3) = 4
    [InlineData(9, 3, 3)]    // tam bölünür
    [InlineData(1, 20, 1)]   // tek kayıt
    [InlineData(0, 20, 0)]   // kayıt yok
    public void PagedResult_TotalPages_CalculatedCorrectly(int totalCount, int pageSize, int expectedPages)
    {
        var result = new PagedResult<string>([], 1, pageSize, totalCount);
        result.TotalPages.ShouldBe(expectedPages);
    }
}
