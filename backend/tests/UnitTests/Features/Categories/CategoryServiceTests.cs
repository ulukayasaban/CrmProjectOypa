using System.Linq.Expressions;
using NSubstitute;
using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Features.Categories;
using Oypa.Crm.Application.Features.Companies;
using Oypa.Crm.Contracts.Categories;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Shouldly;

namespace Oypa.Crm.UnitTests.Features.Categories;

public sealed class CategoryServiceTests
{
    private readonly IRepository<Category> _categories = Substitute.For<IRepository<Category>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CategoryService CreateSut() => new(_categories, _unitOfWork);

    private static Category NewCategory(string name = "Kurumsal", string color = "#3b82f6") =>
        new(name, color);

    // ---- ListAsync ----

    [Fact]
    public async Task ListAsync_ReturnsAllCategories()
    {
        var cat = NewCategory();
        _categories.ListAsync(Arg.Any<CancellationToken>()).Returns(new[] { cat });
        var sut = CreateSut();

        var result = await sut.ListAsync();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Kurumsal");
        result[0].Color.ShouldBe("#3b82f6");
    }

    [Fact]
    public async Task ListAsync_Empty_ReturnsEmptyList()
    {
        _categories.ListAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Category>());
        var sut = CreateSut();

        var result = await sut.ListAsync();

        result.ShouldBeEmpty();
    }

    // ---- CreateAsync ----

    [Fact]
    public async Task CreateAsync_ValidRequest_AddsAndSavesCategory()
    {
        var sut = CreateSut();
        var request = new CreateCategoryRequest("KOBİ", "#22c55e");

        Category? captured = null;
        await _categories.AddAsync(Arg.Do<Category>(c => captured = c), Arg.Any<CancellationToken>());

        var result = await sut.CreateAsync(request);

        result.Name.ShouldBe("KOBİ");
        result.Color.ShouldBe("#22c55e");
        captured.ShouldNotBeNull();
        captured!.Name.ShouldBe("KOBİ");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ---- UpdateAsync ----

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsNotFoundException()
    {
        _categories.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Category?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(() =>
            sut.UpdateAsync(Guid.NewGuid(), new UpdateCategoryRequest("X", "#000000")));
    }

    [Fact]
    public async Task UpdateAsync_ExistingCategory_RenamesAndSetsColor()
    {
        var cat = NewCategory();
        _categories.GetByIdAsync(cat.Id, Arg.Any<CancellationToken>()).Returns(cat);
        var sut = CreateSut();

        var result = await sut.UpdateAsync(cat.Id, new UpdateCategoryRequest("Yeni Ad", "#ef4444"));

        result.Name.ShouldBe("Yeni Ad");
        result.Color.ShouldBe("#ef4444");
        cat.Name.ShouldBe("Yeni Ad");
        cat.Color.ShouldBe("#ef4444");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ---- DeleteAsync ----

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsNotFoundException()
    {
        _categories.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Category?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(() => sut.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_ExistingCategory_MarksDeletedAndSaves()
    {
        var cat = NewCategory();
        _categories.GetByIdAsync(cat.Id, Arg.Any<CancellationToken>()).Returns(cat);
        var sut = CreateSut();

        await sut.DeleteAsync(cat.Id);

        cat.IsDeleted.ShouldBeTrue();
        cat.DeletedAtUtc.ShouldNotBeNull();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public sealed class SetCompanyCategoriesTests
{
    private readonly IRepository<Company> _companies = Substitute.For<IRepository<Company>>();
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();
    private readonly IRepository<SalesRep> _salesReps = Substitute.For<IRepository<SalesRep>>();
    private readonly IRepository<Category> _categories = Substitute.For<IRepository<Category>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CompanyService CreateSut() => new(_companies, _companyRepository, _salesReps, _categories, _unitOfWork);

    private static Company NewLead() =>
        new("Acme", Sector.Retail, "111", "a@b.c", "Adres");

    // ---- SetCategoriesAsync ----

    [Fact]
    public async Task SetCategoriesAsync_CompanyNotFound_ThrowsNotFoundException()
    {
        _companyRepository.GetByIdWithCategoriesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Company?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(() =>
            sut.SetCategoriesAsync(Guid.NewGuid(), new[] { Guid.NewGuid() }));
    }

    [Fact]
    public async Task SetCategoriesAsync_EmptyCategoryIds_ClearsCategories()
    {
        var company = NewLead();

        // First call returns the company (SetCategories step)
        // Second call returns the same company after save (reload step)
        _companyRepository.GetByIdWithCategoriesAsync(company.Id, Arg.Any<CancellationToken>())
            .Returns(company);

        var sut = CreateSut();

        var result = await sut.SetCategoriesAsync(company.Id, Array.Empty<Guid>());

        company.Categories.ShouldBeEmpty();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task SetCategoriesAsync_WithCategoryIds_AssignsCategoriesAndSaves()
    {
        var company = NewLead();
        var cat1 = new Category("Kurumsal", "#3b82f6");
        var cat2 = new Category("KOBİ", "#22c55e");
        var ids = new[] { cat1.Id, cat2.Id };

        _companyRepository.GetByIdWithCategoriesAsync(company.Id, Arg.Any<CancellationToken>())
            .Returns(company);

        _categories.ListAsync(
                Arg.Any<Expression<Func<Category, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(new[] { cat1, cat2 });

        var sut = CreateSut();

        var result = await sut.SetCategoriesAsync(company.Id, ids);

        company.Categories.Count.ShouldBe(2);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        result.Categories.Count.ShouldBe(2);
    }
}
