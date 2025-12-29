using FluentValidation;
using WebApi.Contracts.Auth.Requests;

namespace WebApi.Validation.Auth;

public class RegisterExpertRequestValidator : AbstractValidator<RegisterExpertRequest>
{
    public RegisterExpertRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8);
    }
}
