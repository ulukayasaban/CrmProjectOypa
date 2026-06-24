using System.Linq.Expressions;
using NSubstitute;
using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Features.Contacts;
using Oypa.Crm.Contracts.Contacts;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Shouldly;

namespace Oypa.Crm.UnitTests.Features.Contacts;

public sealed class ContactServiceTests
{
    private readonly IRepository<Company> _companies = Substitute.For<IRepository<Company>>();
    private readonly IRepository<Contact> _contacts = Substitute.For<IRepository<Contact>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private ContactService CreateSut() => new(_companies, _contacts, _unitOfWork);

    private static Company NewCompany() => new("Acme", Sector.Retail, "111", "a@b.c", "Adres");

    [Fact]
    public async Task AddAsync_CompanyNotFound_ThrowsNotFound()
    {
        _companies.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Company?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(() =>
            sut.AddAsync(Guid.NewGuid(), new CreateContactRequest("Ali", "ali@x.com", "555")));

        await _contacts.DidNotReceive().AddAsync(Arg.Any<Contact>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_CompanyExists_AddsContactAndSaves()
    {
        var company = NewCompany();
        _companies.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        var sut = CreateSut();

        var result = await sut.AddAsync(company.Id, new CreateContactRequest("Ali", "ali@x.com", "555"));

        result.Name.ShouldBe("Ali");
        result.CompanyId.ShouldBe(company.Id);
        await _contacts.Received(1).AddAsync(
            Arg.Is<Contact>(c => c.Name == "Ali" && c.CompanyId == company.Id),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByCompanyAsync_FiltersByCompanyId_ReturnsMappedContacts()
    {
        var companyId = Guid.NewGuid();
        Expression<Func<Contact, bool>>? captured = null;
        var match = new Contact(companyId, "Ali", "ali@x.com", "555");
        var other = new Contact(Guid.NewGuid(), "Veli", null, null);

        _contacts.ListAsync(Arg.Any<Expression<Func<Contact, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                captured = ci.Arg<Expression<Func<Contact, bool>>>();
                var predicate = captured.Compile();
                return new[] { match, other }.Where(predicate).ToArray();
            });
        var sut = CreateSut();

        var result = await sut.GetByCompanyAsync(companyId);

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Ali");
        captured.ShouldNotBeNull();
        // Predicate gerçekten CompanyId üzerinden eşleşmeli.
        captured!.Compile()(match).ShouldBeTrue();
        captured.Compile()(other).ShouldBeFalse();
    }

    // -----------------------------------------------------------------------
    // GetByIdAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_ExistingContact_ReturnsDto()
    {
        var companyId = Guid.NewGuid();
        var contact = new Contact(companyId, "Zeynep", "z@x.com", "555");
        _contacts.GetByIdAsync(contact.Id, Arg.Any<CancellationToken>()).Returns(contact);
        var sut = CreateSut();

        var result = await sut.GetByIdAsync(contact.Id);

        result.Id.ShouldBe(contact.Id);
        result.Name.ShouldBe("Zeynep");
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        _contacts.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Contact?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(() => sut.GetByIdAsync(Guid.NewGuid()));
    }

    // -----------------------------------------------------------------------
    // UpdateAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_ExistingContact_UpdatesAndSaves()
    {
        var companyId = Guid.NewGuid();
        var contact = new Contact(companyId, "Eski Ad", "eski@x.com", "111");
        _contacts.GetByIdAsync(contact.Id, Arg.Any<CancellationToken>()).Returns(contact);
        var sut = CreateSut();

        var result = await sut.UpdateAsync(contact.Id, new UpdateContactRequest("Yeni Ad", "yeni@x.com", "222"));

        result.Name.ShouldBe("Yeni Ad");
        result.Email.ShouldBe("yeni@x.com");
        result.Phone.ShouldBe("222");
        _contacts.Received(1).Update(Arg.Is<Contact>(c => c.Name == "Yeni Ad"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsNotFoundAndDoesNotSave()
    {
        _contacts.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Contact?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(() =>
            sut.UpdateAsync(Guid.NewGuid(), new UpdateContactRequest("Ad", null, null)));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // DeleteAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_ExistingContact_RemovesAndSaves()
    {
        var companyId = Guid.NewGuid();
        var contact = new Contact(companyId, "Silinecek", null, null);
        _contacts.GetByIdAsync(contact.Id, Arg.Any<CancellationToken>()).Returns(contact);
        var sut = CreateSut();

        await sut.DeleteAsync(contact.Id);

        _contacts.Received(1).Remove(Arg.Is<Contact>(c => c.Id == contact.Id));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsNotFoundAndDoesNotSave()
    {
        _contacts.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Contact?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(() => sut.DeleteAsync(Guid.NewGuid()));

        _contacts.DidNotReceive().Remove(Arg.Any<Contact>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
