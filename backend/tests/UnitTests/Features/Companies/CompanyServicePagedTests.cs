using NSubstitute;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Features.Companies;
using Oypa.Crm.Contracts.Common;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Shouldly;

namespace Oypa.Crm.UnitTests.Features.Companies;

/// <summary>
/// CompanyService.GetLeadsPagedAsync ve GetCustomersPagedAsync için birim testleri.
/// Sayfa kesimi, arama, sıralama ve toplam kayıt sayısı doğrulanır.
/// </summary>
public sealed class CompanyServicePagedTests
{
    private readonly IRepository<Company> _companies = Substitute.For<IRepository<Company>>();
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();
    private readonly IRepository<SalesRep> _salesReps = Substitute.For<IRepository<SalesRep>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CompanyService CreateSut() => new(_companies, _companyRepository, _salesReps, _unitOfWork);

    private static Company NewLead(string title = "Acme") =>
        new(title, Sector.Retail, "111", "a@b.c", "Adres");

    private static Company NewCustomer(string title = "Acme Müşteri")
    {
        var c = new Company(title, Sector.Retail, "111", "a@b.c", "Adres");
        c.ConvertToCustomer();
        return c;
    }

    // -----------------------------------------------------------------------
    // GetLeadsPagedAsync — sayfa kesimi ve toplam kayıt sayısı
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetLeadsPagedAsync_ReturnsPagedResult_WithCorrectMetadata()
    {
        var leads = new[] { NewLead("A"), NewLead("B") };
        const int totalCount = 7;

        _companyRepository.ListLeadsPagedAsync(
                Arg.Any<LeadStatus?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((leads, totalCount));

        var query = new PagedQuery { Page = 1, PageSize = 2 };
        var sut = CreateSut();

        var result = await sut.GetLeadsPagedAsync(null, query);

        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(2);
        result.TotalCount.ShouldBe(totalCount);
        result.TotalPages.ShouldBe(4); // Math.Ceiling(7/2)
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(2);
    }

    [Fact]
    public async Task GetLeadsPagedAsync_EmptyPage_ReturnsEmptyItems()
    {
        _companyRepository.ListLeadsPagedAsync(
                Arg.Any<LeadStatus?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Company>(), 0));

        var sut = CreateSut();

        var result = await sut.GetLeadsPagedAsync(null, new PagedQuery());

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.TotalPages.ShouldBe(0);
    }

    // -----------------------------------------------------------------------
    // GetLeadsPagedAsync — arama parametresi iletilir
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetLeadsPagedAsync_WithSearch_PassesSearchTermToRepository()
    {
        _companyRepository.ListLeadsPagedAsync(
                Arg.Any<LeadStatus?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Company>(), 0));

        var query = new PagedQuery { Search = "Acme" };
        var sut = CreateSut();

        await sut.GetLeadsPagedAsync(null, query);

        await _companyRepository.Received(1).ListLeadsPagedAsync(
            null, "Acme", Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // GetLeadsPagedAsync — durum filtresi paged uca da taşınır
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetLeadsPagedAsync_WithStatusFilter_PassesStatusToRepository()
    {
        _companyRepository.ListLeadsPagedAsync(
                Arg.Any<LeadStatus?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Company>(), 0));

        var sut = CreateSut();

        await sut.GetLeadsPagedAsync(LeadStatus.Contacted, new PagedQuery());

        await _companyRepository.Received(1).ListLeadsPagedAsync(
            LeadStatus.Contacted, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // GetLeadsPagedAsync — sıralama
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetLeadsPagedAsync_WithSortByTitle_PassesSortToRepository()
    {
        _companyRepository.ListLeadsPagedAsync(
                Arg.Any<LeadStatus?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Company>(), 0));

        var query = new PagedQuery { SortBy = "title", SortDir = "desc" };
        var sut = CreateSut();

        await sut.GetLeadsPagedAsync(null, query);

        await _companyRepository.Received(1).ListLeadsPagedAsync(
            null, Arg.Any<string?>(), "title", true,
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // GetLeadsPagedAsync — DTO dönüşümü
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetLeadsPagedAsync_MapsEntitiesToDtos()
    {
        var lead = NewLead("Acme Özel");
        _companyRepository.ListLeadsPagedAsync(
                Arg.Any<LeadStatus?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((new[] { lead }, 1));

        var sut = CreateSut();

        var result = await sut.GetLeadsPagedAsync(null, new PagedQuery());

        result.Items.Count.ShouldBe(1);
        result.Items[0].Title.ShouldBe("Acme Özel");
        result.Items[0].Type.ShouldBe(CompanyType.Lead);
    }

    // -----------------------------------------------------------------------
    // GetCustomersPagedAsync — sayfa kesimi ve toplam kayıt sayısı
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetCustomersPagedAsync_ReturnsPagedResult_WithCorrectMetadata()
    {
        var customers = new[] { NewCustomer("Müşteri A"), NewCustomer("Müşteri B"), NewCustomer("Müşteri C") };
        const int totalCount = 25;

        _companyRepository.ListCustomersPagedAsync(
                Arg.Any<CustomerStatus?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((customers, totalCount));

        var query = new PagedQuery { Page = 2, PageSize = 10 };
        var sut = CreateSut();

        var result = await sut.GetCustomersPagedAsync(null, query);

        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(3);
        result.TotalCount.ShouldBe(totalCount);
        result.TotalPages.ShouldBe(3); // Math.Ceiling(25/10)
        result.Page.ShouldBe(2);
        result.PageSize.ShouldBe(10);
    }

    [Fact]
    public async Task GetCustomersPagedAsync_WithStatusFilter_PassesStatusToRepository()
    {
        _companyRepository.ListCustomersPagedAsync(
                Arg.Any<CustomerStatus?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Company>(), 0));

        var sut = CreateSut();

        await sut.GetCustomersPagedAsync(CustomerStatus.Active, new PagedQuery());

        await _companyRepository.Received(1).ListCustomersPagedAsync(
            CustomerStatus.Active, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCustomersPagedAsync_MapsEntitiesToDtos_WithCustomerType()
    {
        var customer = NewCustomer("Süper Müşteri");
        _companyRepository.ListCustomersPagedAsync(
                Arg.Any<CustomerStatus?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((new[] { customer }, 1));

        var sut = CreateSut();

        var result = await sut.GetCustomersPagedAsync(null, new PagedQuery());

        result.Items.Count.ShouldBe(1);
        result.Items[0].Title.ShouldBe("Süper Müşteri");
        result.Items[0].Type.ShouldBe(CompanyType.Customer);
    }
}
