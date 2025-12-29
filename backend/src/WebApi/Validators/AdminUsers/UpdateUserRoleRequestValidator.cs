using Domain.Constants;
using FluentValidation;
using WebApi.Contracts.AdminUsers;

namespace WebApi.Validators.AdminUsers;

public class UpdateUserRoleRequestValidator : AbstractValidator<UpdateUserRoleRequest>
{
    private static readonly HashSet<string> AllowedRoles = new()
    {
        RoleNames.Admin,
        RoleNames.Expert,
        RoleNames.Company
    };

    public UpdateUserRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(role => AllowedRoles.Contains(role))
            .WithMessage("Role is invalid.");
    }
}
