using System.Linq.Expressions;
using NSubstitute;
using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Features.Companies;
using Oypa.Crm.Contracts.Companies;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Shouldly;
using ServiceSector = Oypa.Crm.Domain.Enums.ServiceSector;

namespace Oypa.Crm.UnitTests.Features.Companies;

public sealed class CompanyServiceTests
{
    private readonly IRepository<Company> _companies = Substitute.For<IRepository<Company>>();
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();
    private readonly IRepository<SalesRep> _salesReps = Substitute.For<IRepository<SalesRep>>();
    private readonly IRepository<Category> _categories = Substitute.For<IRepository<Category>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CompanyService CreateSut() => new(_companies, _companyRepository, _salesReps, _categories, _unitOfWork);

    private static Company NewLead() =>
        new("Acme", Sector.Retail, "111", "a@b.c", "Adres");

    private static Company NewLeadWithRevize() =>
        new("Acme",
            Sector.Retail,
            "111",
            "a@b.c",
            "Adres",
            serviceSector: ServiceSector.TesisYonetimi,
            firmType: FirmType.IcFirma,
            sourceNote: "Test notu");

    // ---- mevcut testler ----

    [Fact]
    public async Task CreateAsync_NewCompany_IsLeadWithNewStatus()
    {
        var sut = CreateSut();

        var result = await sut.CreateAsync(new CreateCompanyRequest("Acme", Sector.Retail, "111", "a@b.c", "Adres"));

        result.Type.ShouldBe(CompanyType.Lead);
        result.LeadStatus.ShouldBe(LeadStatus.New);
        await _companies.Received(1).AddAsync(Arg.Any<Company>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConvertToCustomerAsync_AlreadyCustomer_ThrowsConflict()
    {
        var company = NewLead();
        company.ConvertToCustomer();
        _companies.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        var sut = CreateSut();

        await Should.ThrowAsync<ConflictException>(() => sut.ConvertToCustomerAsync(company.Id));
    }

    [Fact]
    public async Task SetLeadStatusAsync_CompanyIsCustomer_ThrowsConflict()
    {
        var company = NewLead();
        company.ConvertToCustomer();
        _companies.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        var sut = CreateSut();

        await Should.ThrowAsync<ConflictException>(() => sut.SetLeadStatusAsync(company.Id, LeadStatus.Contacted));
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFound()
    {
        _companyRepository.GetByIdWithRepAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Company?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(() => sut.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetLeadsAsync_ReturnsMappedLeads()
    {
        var lead = NewLead();
        _companyRepository.ListLeadsAsync(Arg.Any<LeadStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new[] { lead });
        var sut = CreateSut();

        var result = await sut.GetLeadsAsync();

        result.Count.ShouldBe(1);
        result[0].Title.ShouldBe("Acme");
    }

    // ---- yeni alan testleri (City / Website / TaxNumber / Source) ----

    [Fact]
    public async Task CreateAsync_WithOptionalFields_MapsAllFieldsToDto()
    {
        var sut = CreateSut();
        var request = new CreateCompanyRequest(
            "Acme",
            Sector.Retail,
            "111",
            "a@b.c",
            "Adres",
            City: "Ankara",
            Website: "https://acme.com",
            TaxNumber: "1234567890",
            Source: CompanySource.Referral);

        Company? captured = null;
        await _companies.AddAsync(Arg.Do<Company>(c => captured = c), Arg.Any<CancellationToken>());

        var result = await sut.CreateAsync(request);

        // DTO alanları doğru taşınmalı
        result.City.ShouldBe("Ankara");
        result.Website.ShouldBe("https://acme.com");
        result.TaxNumber.ShouldBe("1234567890");
        result.Source.ShouldBe(CompanySource.Referral);

        // Entity'ye de set edilmeli
        captured.ShouldNotBeNull();
        captured!.City.ShouldBe("Ankara");
        captured.Website.ShouldBe("https://acme.com");
        captured.TaxNumber.ShouldBe("1234567890");
        captured.Source.ShouldBe(CompanySource.Referral);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithoutOptionalFields_NullFieldsInDto()
    {
        var sut = CreateSut();
        var request = new CreateCompanyRequest("Acme", Sector.Retail, "111", "a@b.c", "Adres");

        var result = await sut.CreateAsync(request);

        result.City.ShouldBeNull();
        result.Website.ShouldBeNull();
        result.TaxNumber.ShouldBeNull();
        result.Source.ShouldBeNull();
    }

    // ---- SetCustomerStatusAsync testleri ----

    [Fact]
    public async Task SetCustomerStatusAsync_CompanyNotFound_ThrowsNotFound()
    {
        _companies.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Company?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(() =>
            sut.SetCustomerStatusAsync(Guid.NewGuid(), CustomerStatus.Passive));
    }

    [Fact]
    public async Task SetCustomerStatusAsync_CompanyIsLead_ThrowsConflict()
    {
        var lead = NewLead();
        _companies.GetByIdAsync(lead.Id, Arg.Any<CancellationToken>()).Returns(lead);
        var sut = CreateSut();

        await Should.ThrowAsync<ConflictException>(() =>
            sut.SetCustomerStatusAsync(lead.Id, CustomerStatus.Passive));
    }

    [Fact]
    public async Task SetCustomerStatusAsync_CompanyIsCustomer_UpdatesStatusAndSaves()
    {
        var company = NewLead();
        company.ConvertToCustomer();
        _companies.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        var sut = CreateSut();

        await sut.SetCustomerStatusAsync(company.Id, CustomerStatus.Passive);

        company.CustomerStatus.ShouldBe(CustomerStatus.Passive);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ---- GetLeadsAsync filtre testleri ----

    [Fact]
    public async Task GetLeadsAsync_WithStatusFilter_CallsRepoWithPredicate()
    {
        var matchingLead = NewLead();
        matchingLead.SetLeadStatus(LeadStatus.Contacted);

        _companyRepository.ListLeadsAsync(Arg.Any<LeadStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new[] { matchingLead });
        var sut = CreateSut();

        var result = await sut.GetLeadsAsync(LeadStatus.Contacted);

        result.Count.ShouldBe(1);
        result[0].LeadStatus.ShouldBe(LeadStatus.Contacted);
        await _companyRepository.Received(1).ListLeadsAsync(Arg.Any<LeadStatus?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetLeadsAsync_NoFilter_CallsRepoOnce()
    {
        _companyRepository.ListLeadsAsync(Arg.Any<LeadStatus?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Company>());
        var sut = CreateSut();

        var result = await sut.GetLeadsAsync();

        result.ShouldBeEmpty();
        await _companyRepository.Received(1).ListLeadsAsync(Arg.Any<LeadStatus?>(), Arg.Any<CancellationToken>());
    }

    // ---- GetCustomersAsync filtre testleri ----

    [Fact]
    public async Task GetCustomersAsync_WithStatusFilter_CallsRepoWithPredicate()
    {
        var customer = NewLead();
        customer.ConvertToCustomer();
        customer.SetCustomerStatus(CustomerStatus.Passive);

        _companyRepository.ListCustomersAsync(Arg.Any<CustomerStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new[] { customer });
        var sut = CreateSut();

        var result = await sut.GetCustomersAsync(CustomerStatus.Passive);

        result.Count.ShouldBe(1);
        result[0].CustomerStatus.ShouldBe(CustomerStatus.Passive);
        await _companyRepository.Received(1).ListCustomersAsync(Arg.Any<CustomerStatus?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCustomersAsync_NoFilter_CallsRepoOnce()
    {
        _companyRepository.ListCustomersAsync(Arg.Any<CustomerStatus?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Company>());
        var sut = CreateSut();

        var result = await sut.GetCustomersAsync();

        result.ShouldBeEmpty();
        await _companyRepository.Received(1).ListCustomersAsync(Arg.Any<CustomerStatus?>(), Arg.Any<CancellationToken>());
    }

    // ---- AssignSalesRepAsync testleri ----

    [Fact]
    public async Task AssignSalesRepAsync_CompanyNotFound_ThrowsNotFound()
    {
        _companyRepository.GetByIdWithRepAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Company?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(() => sut.AssignSalesRepAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task AssignSalesRepAsync_SalesRepNotFound_ThrowsNotFound()
    {
        var company = NewLead();
        _companyRepository.GetByIdWithRepAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _salesReps.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((SalesRep?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(() => sut.AssignSalesRepAsync(company.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task AssignSalesRepAsync_ValidRep_AssignsAndSaves()
    {
        var company = NewLead();
        var rep = new SalesRep("Ali Veli", "ali@oypa.com");
        _companyRepository.GetByIdWithRepAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _salesReps.GetByIdAsync(rep.Id, Arg.Any<CancellationToken>()).Returns(rep);
        var sut = CreateSut();

        await sut.AssignSalesRepAsync(company.Id, rep.Id);

        company.AssignedSalesRepId.ShouldBe(rep.Id);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignSalesRepAsync_NullRepId_UnassignsAndSaves()
    {
        var company = NewLead();
        company.AssignSalesRep(Guid.NewGuid());
        _companyRepository.GetByIdWithRepAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        var sut = CreateSut();

        await sut.AssignSalesRepAsync(company.Id, null);

        company.AssignedSalesRepId.ShouldBeNull();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ---- Company entity unit testleri: revize alanlar ----

    [Fact]
    public void Company_Ctor_DefaultFirmType_IsDisFirma()
    {
        var company = new Company("Test A.Ş.", Sector.Retail, "111", "t@t.com", "Adres");

        company.FirmType.ShouldBe(FirmType.DisFirma);
        company.ServiceSector.ShouldBeNull();
        company.SourceNote.ShouldBeNull();
        company.LeadOwnerId.ShouldBeNull();
    }

    [Fact]
    public void Company_Ctor_WithRevizeFields_SetsAllFields()
    {
        var company = new Company(
            "Test A.Ş.",
            Sector.FacilityManagement,
            "111",
            "t@t.com",
            "Adres",
            serviceSector: ServiceSector.TesisYonetimi,
            firmType: FirmType.IcFirma,
            sourceNote: "Belgin Öner referansı");

        company.ServiceSector.ShouldBe(ServiceSector.TesisYonetimi);
        company.FirmType.ShouldBe(FirmType.IcFirma);
        company.SourceNote.ShouldBe("Belgin Öner referansı");
    }

    [Fact]
    public void Company_UpdateDetails_UpdatesRevizeFields()
    {
        var company = new Company("Test A.Ş.", Sector.Retail, "111", "t@t.com", "Adres");

        company.UpdateDetails(
            "Test A.Ş.",
            Sector.Retail,
            "111",
            "t@t.com",
            "Adres",
            serviceSector: ServiceSector.Turizm,
            firmType: FirmType.IcFirma,
            sourceNote: "Güncelleme notu");

        company.ServiceSector.ShouldBe(ServiceSector.Turizm);
        company.FirmType.ShouldBe(FirmType.IcFirma);
        company.SourceNote.ShouldBe("Güncelleme notu");
    }

    [Fact]
    public void Company_SetLeadOwner_SetsLeadOwnerId()
    {
        var company = new Company("Test A.Ş.", Sector.Retail, "111", "t@t.com", "Adres");
        var repId = Guid.NewGuid();

        company.SetLeadOwner(repId);

        company.LeadOwnerId.ShouldBe(repId);
    }

    [Fact]
    public void Company_SetLeadOwner_NullClearsLeadOwner()
    {
        var company = new Company("Test A.Ş.", Sector.Retail, "111", "t@t.com", "Adres");
        company.SetLeadOwner(Guid.NewGuid());

        company.SetLeadOwner(null);

        company.LeadOwnerId.ShouldBeNull();
    }

    // ---- CreateAsync ile revize alanlar DTO'ya taşınıyor ----

    [Fact]
    public async Task CreateAsync_WithRevizeFields_MapsToDto()
    {
        var sut = CreateSut();
        var request = new CreateCompanyRequest(
            "Revize A.Ş.",
            Sector.FacilityManagement,
            "222",
            "revize@b.com",
            "Adres",
            ServiceSector: ServiceSector.TesisYonetimi,
            FirmType: FirmType.IcFirma,
            SourceNote: "Test kaynağı");

        Company? captured = null;
        await _companies.AddAsync(Arg.Do<Company>(c => captured = c), Arg.Any<CancellationToken>());

        var result = await sut.CreateAsync(request);

        result.ServiceSector.ShouldBe(ServiceSector.TesisYonetimi);
        result.FirmType.ShouldBe(FirmType.IcFirma);
        result.SourceNote.ShouldBe("Test kaynağı");
        result.LeadOwnerId.ShouldBeNull();

        captured.ShouldNotBeNull();
        captured!.ServiceSector.ShouldBe(ServiceSector.TesisYonetimi);
        captured.FirmType.ShouldBe(FirmType.IcFirma);
        captured.SourceNote.ShouldBe("Test kaynağı");

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ---- SetLeadOwnerAsync service metodu ----

    [Fact]
    public async Task SetLeadOwnerAsync_CompanyNotFound_ThrowsNotFound()
    {
        _companyRepository.GetByIdWithRepAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Company?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(() => sut.SetLeadOwnerAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task SetLeadOwnerAsync_LeadOwnerRepNotFound_ThrowsNotFound()
    {
        var company = NewLead();
        _companyRepository.GetByIdWithRepAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _salesReps.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((SalesRep?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(() => sut.SetLeadOwnerAsync(company.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task SetLeadOwnerAsync_ValidRep_SetsLeadOwnerAndSaves()
    {
        var company = NewLead();
        var rep = new SalesRep("Lead Owner", "lo@oypa.com");
        _companyRepository.GetByIdWithRepAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _salesReps.GetByIdAsync(rep.Id, Arg.Any<CancellationToken>()).Returns(rep);
        var sut = CreateSut();

        await sut.SetLeadOwnerAsync(company.Id, rep.Id);

        company.LeadOwnerId.ShouldBe(rep.Id);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetLeadOwnerAsync_NullRepId_ClearsLeadOwnerAndSaves()
    {
        var company = NewLead();
        company.SetLeadOwner(Guid.NewGuid());
        _companyRepository.GetByIdWithRepAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        var sut = CreateSut();

        await sut.SetLeadOwnerAsync(company.Id, null);

        company.LeadOwnerId.ShouldBeNull();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
