using FluentValidation;
using Oypa.Crm.Contracts.Categories;

namespace Oypa.Crm.Application.Features.Categories.Validators;

public sealed class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    private const string HexColorPattern = @"^#([0-9A-Fa-f]{6})$";

    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Color).NotEmpty().Matches(HexColorPattern)
            .WithMessage("Renk geçerli bir hex kodu olmalıdır (ör: #3b82f6).");
    }
}
