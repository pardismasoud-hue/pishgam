using FluentValidation;
using WebApi.Contracts.Contracts;

namespace WebApi.Validation.Contracts;

public class ContractServiceRequestValidator : AbstractValidator<ContractServiceRequest>
{
    public ContractServiceRequestValidator()
    {
        RuleFor(x => x.ServiceId)
            .NotEmpty();

        When(x => x.CustomFirstResponseMinutes.HasValue || x.CustomResolutionMinutes.HasValue, () =>
        {
            RuleFor(x => x.CustomFirstResponseMinutes)
                .NotNull()
                .GreaterThan(0);

            RuleFor(x => x.CustomResolutionMinutes)
                .NotNull()
                .GreaterThan(0)
                .GreaterThanOrEqualTo(x => x.CustomFirstResponseMinutes!.Value);
        });
    }
}
