using Domain.Constants;
using FluentValidation;
using WebApi.Contracts.AdminUsers;

namespace WebApi.Validators.AdminUsers;

public class CreateAdminUserRequestValidator : AbstractValidator<CreateAdminUserRequest>
{
    private static readonly HashSet<string> AllowedRoles = new()
    {
        RoleNames.Admin,
        RoleNames.Expert,
        RoleNames.Company
    };

    public CreateAdminUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(6)
            .MaximumLength(100);

        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(role => AllowedRoles.Contains(role))
            .WithMessage("Role is invalid.");
    }
}
