using NSubstitute;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Features.SalesReps;
using Oypa.Crm.Contracts.SalesReps;
using Oypa.Crm.Domain.Entities;
using Shouldly;

namespace Oypa.Crm.UnitTests.Features.SalesReps;

public sealed class SalesRepServiceTests
{
    private readonly IRepository<SalesRep> _salesReps = Substitute.For<IRepository<SalesRep>>();
    private readonly IRepository<Employee> _employees = Substitute.For<IRepository<Employee>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private SalesRepService CreateSut() => new(_salesReps, _employees, _unitOfWork);

    [Fact]
    public async Task CreateAsync_AddsRepAndSaves()
    {
        var sut = CreateSut();

        var result = await sut.CreateAsync(new CreateSalesRepRequest("Ayse", "ayse@oypa.com"));

        result.Name.ShouldBe("Ayse");
        result.Email.ShouldBe("ayse@oypa.com");
        await _salesReps.Received(1).AddAsync(
            Arg.Is<SalesRep>(r => r.Name == "Ayse" && r.Email == "ayse@oypa.com"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedReps()
    {
        var rep = new SalesRep("Ayse", "ayse@oypa.com");
        _salesReps.ListAsync(Arg.Any<CancellationToken>()).Returns(new[] { rep });
        var sut = CreateSut();

        var result = await sut.GetAllAsync();

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(rep.Id);
        result[0].Name.ShouldBe("Ayse");
        result[0].Email.ShouldBe("ayse@oypa.com");
    }
}
