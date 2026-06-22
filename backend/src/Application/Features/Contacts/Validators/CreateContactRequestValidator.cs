using FluentValidation;
using Oypa.Crm.Contracts.Contacts;

namespace Oypa.Crm.Application.Features.Contacts.Validators;

public sealed class CreateContactRequestValidator : AbstractValidator<CreateContactRequest>
{
    public CreateContactRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(30)
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}
