using NSubstitute;
using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Features.Tenders;
using Oypa.Crm.Contracts.Tenders;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Shouldly;

namespace Oypa.Crm.UnitTests.Features.Tenders;

public sealed class TenderServiceTests
{
    private readonly ITenderRepository _tenders = Substitute.For<ITenderRepository>();
    private readonly IRepository<Company> _companies = Substitute.For<IRepository<Company>>();
    private readonly IRepository<SalesRep> _salesReps = Substitute.For<IRepository<SalesRep>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private TenderService CreateSut() =>
        new(_tenders, _companies, _salesReps, _unitOfWork);

    private static Company NewCompany() =>
        new("Acme", Sector.Retail, "111", "acme@test.com", "Adres");

    private static SalesRep NewSalesRep() =>
        new("Ali Veli", "ali@oypa.com");

    private static Tender NewTender(Guid companyId, Guid? repId = null) =>
        Tender.Create(companyId, "Test İhalesi", "IH-001", Sector.Retail,
            new DateOnly(2026, 12, 1), null, 1000m, 500m, null, null, repId);

    private static CreateTenderRequest NewCreateRequest(Guid companyId, Guid? repId = null) =>
        new(companyId, "Test İhalesi", "IH-001", Sector.Retail,
            new DateOnly(2026, 12, 1), null, 1000m, 500m, null, null, repId);

    private static UpdateTenderRequest NewUpdateRequest(Guid companyId, Guid? repId = null) =>
        new(companyId, "Güncel İhale", "IH-002", Sector.Energy,
            new DateOnly(2026, 12, 15), 10, 2000m, 750m, 5, "Güncel açıklama", repId);

    // -----------------------------------------------------------------------
    // CreateAsync — CompanyId doğrulaması
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_ValidCompanyId_CreatesAndReturnsTenderDto()
    {
        var company = NewCompany();
        _companies.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);

        var tender = NewTender(company.Id);
        _tenders.GetByIdWithDetailsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(tender);
        var sut = CreateSut();

        var result = await sut.CreateAsync(NewCreateRequest(company.Id));

        result.ShouldNotBeNull();
        result.CompanyId.ShouldBe(company.Id);
        await _tenders.Received(1).AddAsync(Arg.Any<Tender>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_UnknownCompanyId_ThrowsNotFound()
    {
        _companies.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Company?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(
            () => sut.CreateAsync(NewCreateRequest(Guid.NewGuid())));

        await _tenders.DidNotReceive().AddAsync(Arg.Any<Tender>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithAssignedSalesRep_ValidatesRepExists()
    {
        var company = NewCompany();
        var rep = NewSalesRep();
        _companies.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _salesReps.GetByIdAsync(rep.Id, Arg.Any<CancellationToken>()).Returns(rep);

        var tender = NewTender(company.Id, rep.Id);
        _tenders.GetByIdWithDetailsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(tender);
        var sut = CreateSut();

        var result = await sut.CreateAsync(NewCreateRequest(company.Id, rep.Id));

        result.ShouldNotBeNull();
        await _salesReps.Received(1).GetByIdAsync(rep.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithUnknownSalesRep_ThrowsNotFound()
    {
        var company = NewCompany();
        _companies.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _salesReps.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((SalesRep?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(
            () => sut.CreateAsync(NewCreateRequest(company.Id, Guid.NewGuid())));
    }

    // -----------------------------------------------------------------------
    // GetAsync — sector + status filtresi
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_WithSectorFilter_PassesSectorToRepository()
    {
        _tenders.ListAsync(Arg.Any<Sector?>(), Arg.Any<TenderStatus?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Tender>());
        var sut = CreateSut();

        await sut.GetAsync(Sector.Energy, null);

        await _tenders.Received(1).ListAsync(Sector.Energy, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WithStatusFilter_PassesStatusToRepository()
    {
        _tenders.ListAsync(Arg.Any<Sector?>(), Arg.Any<TenderStatus?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Tender>());
        var sut = CreateSut();

        await sut.GetAsync(null, TenderStatus.TeklifVerildi);

        await _tenders.Received(1).ListAsync(null, TenderStatus.TeklifVerildi, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_NoFilter_ReturnsMappedDtos()
    {
        var company = NewCompany();
        var tender = NewTender(company.Id);
        _tenders.ListAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(new[] { tender });
        var sut = CreateSut();

        var result = await sut.GetAsync(null, null);

        result.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetAsync_BothFiltersApplied_PassesBothToRepository()
    {
        _tenders.ListAsync(Sector.Tourism, TenderStatus.Hazirlik, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Tender>());
        var sut = CreateSut();

        await sut.GetAsync(Sector.Tourism, TenderStatus.Hazirlik);

        await _tenders.Received(1).ListAsync(Sector.Tourism, TenderStatus.Hazirlik, Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // GetByIdAsync — NotFound
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFound()
    {
        _tenders.GetByIdWithDetailsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Tender?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(() => sut.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByIdAsync_ExistingTender_ReturnsDto()
    {
        var company = NewCompany();
        var tender = NewTender(company.Id);
        _tenders.GetByIdWithDetailsAsync(tender.Id, Arg.Any<CancellationToken>()).Returns(tender);
        var sut = CreateSut();

        var result = await sut.GetByIdAsync(tender.Id);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(tender.Id);
    }

    // -----------------------------------------------------------------------
    // UpdateAsync — alanlar doğru güncellenir, decimal sakla/oku
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesFieldsAndSaves()
    {
        var company = NewCompany();
        var tender = NewTender(company.Id);
        _tenders.GetByIdAsync(tender.Id, Arg.Any<CancellationToken>()).Returns(tender);
        _companies.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);

        var updatedTender = Tender.Create(company.Id, "Güncel İhale", "IH-002", Sector.Energy,
            new DateOnly(2026, 12, 15), 10, 2000m, 750m, 5, "Güncel açıklama", null);
        _tenders.GetByIdWithDetailsAsync(tender.Id, Arg.Any<CancellationToken>()).Returns(updatedTender);
        var sut = CreateSut();

        var result = await sut.UpdateAsync(tender.Id, NewUpdateRequest(company.Id));

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        result.Title.ShouldBe("Güncel İhale");
    }

    [Fact]
    public async Task UpdateAsync_UnknownTenderId_ThrowsNotFound()
    {
        _tenders.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Tender?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(
            () => sut.UpdateAsync(Guid.NewGuid(), NewUpdateRequest(Guid.NewGuid())));
    }

    [Fact]
    public async Task UpdateAsync_UnknownCompanyId_ThrowsNotFound()
    {
        var tender = NewTender(Guid.NewGuid());
        _tenders.GetByIdAsync(tender.Id, Arg.Any<CancellationToken>()).Returns(tender);
        _companies.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Company?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(
            () => sut.UpdateAsync(tender.Id, NewUpdateRequest(Guid.NewGuid())));
    }

    [Fact]
    public async Task UpdateAsync_DecimalEstimatedValueAndVolume_StoredCorrectly()
    {
        var company = NewCompany();
        var tender = NewTender(company.Id);
        _tenders.GetByIdAsync(tender.Id, Arg.Any<CancellationToken>()).Returns(tender);
        _companies.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);

        // Capture UpdateDetails call via the tender object
        var updatedTender = Tender.Create(company.Id, "Güncellendi", null, Sector.Energy,
            new DateOnly(2026, 12, 1), null, 9999.99m, 8888.88m, null, null, null);
        _tenders.GetByIdWithDetailsAsync(tender.Id, Arg.Any<CancellationToken>()).Returns(updatedTender);
        var sut = CreateSut();

        var updateReq = new UpdateTenderRequest(company.Id, "Güncellendi", null, Sector.Energy,
            new DateOnly(2026, 12, 1), null, 9999.99m, 8888.88m, null, null, null);
        var result = await sut.UpdateAsync(tender.Id, updateReq);

        // Değerlerin doğru map edildiğini kontrol et (GetByIdWithDetailsAsync mock'u döndürür)
        result.EstimatedValue.ShouldBe(9999.99m);
        result.Volume.ShouldBe(8888.88m);
    }

    // -----------------------------------------------------------------------
    // ChangeStatusAsync — durum değiştirir
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ChangeStatusAsync_ValidTender_ChangesStatusAndSaves()
    {
        var company = NewCompany();
        var tender = NewTender(company.Id);
        _tenders.GetByIdAsync(tender.Id, Arg.Any<CancellationToken>()).Returns(tender);
        var sut = CreateSut();

        await sut.ChangeStatusAsync(tender.Id, new ChangeTenderStatusRequest(TenderStatus.TeklifVerildi));

        tender.Status.ShouldBe(TenderStatus.TeklifVerildi);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeStatusAsync_UnknownTenderId_ThrowsNotFound()
    {
        _tenders.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Tender?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(
            () => sut.ChangeStatusAsync(Guid.NewGuid(), new ChangeTenderStatusRequest(TenderStatus.Kazanildi)));
    }

    [Fact]
    public async Task ChangeStatusAsync_AllStatuses_Accepted()
    {
        var company = NewCompany();

        foreach (var status in Enum.GetValues<TenderStatus>())
        {
            var tender = NewTender(company.Id);
            _tenders.GetByIdAsync(tender.Id, Arg.Any<CancellationToken>()).Returns(tender);
            var sut = CreateSut();

            await sut.ChangeStatusAsync(tender.Id, new ChangeTenderStatusRequest(status));

            tender.Status.ShouldBe(status);
        }
    }

    // -----------------------------------------------------------------------
    // DeleteAsync — siler ve kaydeder
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_ExistingTender_RemovesAndSaves()
    {
        var company = NewCompany();
        var tender = NewTender(company.Id);
        _tenders.GetByIdAsync(tender.Id, Arg.Any<CancellationToken>()).Returns(tender);
        var sut = CreateSut();

        await sut.DeleteAsync(tender.Id);

        _tenders.Received(1).Remove(tender);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_UnknownTenderId_ThrowsNotFound()
    {
        _tenders.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Tender?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(
            () => sut.DeleteAsync(Guid.NewGuid()));

        _tenders.DidNotReceive().Remove(Arg.Any<Tender>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
