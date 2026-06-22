using FluentValidation.TestHelper;
using Oypa.Crm.Application.Features.Auth.Validators;
using Oypa.Crm.Application.Features.Companies.Validators;
using Oypa.Crm.Application.Features.Goals.Validators;
using Oypa.Crm.Application.Features.Meetings.Validators;
using Oypa.Crm.Contracts.Auth;
using Oypa.Crm.Contracts.Companies;
using Oypa.Crm.Contracts.Goals;
using Oypa.Crm.Contracts.Meetings;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.UnitTests.Features.Validators;

public sealed class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Validate_EmptyEmailAndPassword_HasErrors()
    {
        var result = _validator.TestValidate(new LoginRequest("", ""));

        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_InvalidEmailFormat_HasError()
    {
        var result = _validator.TestValidate(new LoginRequest("not-an-email", "secret"));

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ValidRequest_NoErrors()
    {
        var result = _validator.TestValidate(new LoginRequest("user@oypa.com", "secret"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}

public sealed class CreateCompanyRequestValidatorTests
{
    private readonly CreateCompanyRequestValidator _validator = new();

    private static CreateCompanyRequest Valid() =>
        new("Acme A.Ş.", Sector.Retail, "0212", "a@b.com", "Adres");

    [Fact]
    public void Validate_EmptyTitle_HasError()
    {
        var result = _validator.TestValidate(Valid() with { Title = "" });

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_InvalidEmail_HasError()
    {
        var result = _validator.TestValidate(Valid() with { Email = "invalid" });

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_SectorOutOfEnumRange_HasError()
    {
        var result = _validator.TestValidate(Valid() with { Sector = (Sector)123 });

        result.ShouldHaveValidationErrorFor(x => x.Sector);
    }

    [Fact]
    public void Validate_ValidRequest_NoErrors()
    {
        var result = _validator.TestValidate(Valid());

        result.ShouldNotHaveAnyValidationErrors();
    }
}

public sealed class ScheduleMeetingRequestValidatorTests
{
    private readonly ScheduleMeetingRequestValidator _validator = new();

    private static ScheduleMeetingRequest Valid() => new(
        Guid.NewGuid(), null, Guid.NewGuid(),
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
        new TimeOnly(10, 0), "Adres", MeetingMethod.Visit);

    [Fact]
    public void Validate_EmptyCompanyId_HasError()
    {
        var result = _validator.TestValidate(Valid() with { CompanyId = Guid.Empty });

        result.ShouldHaveValidationErrorFor(x => x.CompanyId);
    }

    [Fact]
    public void Validate_EmptySalesRepId_HasError()
    {
        var result = _validator.TestValidate(Valid() with { SalesRepId = Guid.Empty });

        result.ShouldHaveValidationErrorFor(x => x.SalesRepId);
    }

    [Fact]
    public void Validate_InvalidMethod_HasError()
    {
        var result = _validator.TestValidate(Valid() with { Method = (MeetingMethod)99 });

        result.ShouldHaveValidationErrorFor(x => x.Method);
    }

    [Fact]
    public void Validate_ValidRequest_NoErrors()
    {
        var result = _validator.TestValidate(Valid());

        result.ShouldNotHaveAnyValidationErrors();
    }
}

public sealed class RegisterUserRequestValidatorTests
{
    private readonly RegisterUserRequestValidator _validator = new();

    private static RegisterUserRequest Valid() =>
        new("user@oypa.com", "Parola12", "Tam Ad", "Sales");

    [Fact]
    public void Validate_PasswordShorterThanMinimum_HasError()
    {
        var result = _validator.TestValidate(Valid() with { Password = "short1" });

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_PasswordAtMinimumLength_NoPasswordError()
    {
        var result = _validator.TestValidate(Valid() with { Password = "12345678" });

        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_ValidRequest_NoErrors()
    {
        var result = _validator.TestValidate(Valid());

        result.ShouldNotHaveAnyValidationErrors();
    }
}

public sealed class CreateGoalRequestValidatorTests
{
    private readonly CreateGoalRequestValidator _validator = new();

    private static CreateGoalRequest Valid() =>
        new(Guid.NewGuid(), "All", 5, null);

    [Fact]
    public void Validate_EmptyAssigneeEmployeeId_HasError()
    {
        var result = _validator.TestValidate(Valid() with { AssigneeEmployeeId = Guid.Empty });

        result.ShouldHaveValidationErrorFor(x => x.AssigneeEmployeeId);
    }

    [Fact]
    public void Validate_ZeroWeeklyTarget_HasError()
    {
        var result = _validator.TestValidate(Valid() with { WeeklyTarget = 0 });

        result.ShouldHaveValidationErrorFor(x => x.WeeklyTarget);
    }

    [Fact]
    public void Validate_NegativeWeeklyTarget_HasError()
    {
        var result = _validator.TestValidate(Valid() with { WeeklyTarget = -1 });

        result.ShouldHaveValidationErrorFor(x => x.WeeklyTarget);
    }

    [Fact]
    public void Validate_InvalidSegment_HasError()
    {
        var result = _validator.TestValidate(Valid() with { Segment = "Unknown" });

        result.ShouldHaveValidationErrorFor(x => x.Segment);
    }

    [Fact]
    public void Validate_ValidRequest_NoErrors()
    {
        var result = _validator.TestValidate(Valid());

        result.ShouldNotHaveAnyValidationErrors();
    }
}
