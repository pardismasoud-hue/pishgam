using FluentValidation;
using WebApi.Contracts.Services;

namespace WebApi.Validation.Services;

public class CreateServiceRequestValidator : AbstractValidator<CreateServiceRequest>
{
    public CreateServiceRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        RuleFor(x => x.DefaultFirstResponseMinutes)
            .GreaterThan(0);

        RuleFor(x => x.DefaultResolutionMinutes)
            .GreaterThan(0)
            .GreaterThanOrEqualTo(x => x.DefaultFirstResponseMinutes);
    }
}
