using FluentValidation;
using WebApi.Contracts.Contracts;

namespace WebApi.Validation.Contracts;

public class UpdateContractRequestValidator : AbstractValidator<UpdateContractRequest>
{
    public UpdateContractRequestValidator()
    {
        RuleFor(x => x.MonthlyPrice)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.IncludedSupportMinutesPerMonth)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.OnsiteDaysIncluded)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Services)
            .NotEmpty();

        RuleForEach(x => x.Services)
            .SetValidator(new ContractServiceRequestValidator());

        RuleFor(x => x.AssetIds)
            .Must(x => x.Distinct().Count() == x.Count)
            .WithMessage("AssetIds must be unique.");

        RuleFor(x => x.Services)
            .Must(x => x.Select(s => s.ServiceId).Distinct().Count() == x.Count)
            .WithMessage("ServiceId values must be unique.");
    }
}
