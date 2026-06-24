using FluentValidation;
using Oypa.Crm.Contracts.Contacts;

namespace Oypa.Crm.Application.Features.Contacts.Validators;

/// <summary>UpdateContactRequest için doğrulama kuralları. CreateContactRequest deseniyle tutarlıdır.</summary>
public sealed class UpdateContactRequestValidator : AbstractValidator<UpdateContactRequest>
{
    public UpdateContactRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(30)
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}
