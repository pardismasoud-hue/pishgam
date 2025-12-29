using FluentValidation;
using WebApi.Contracts.Experts;

namespace WebApi.Validation.Experts;

public class ExpertSkillUpdateRequestValidator : AbstractValidator<ExpertSkillUpdateRequest>
{
    public ExpertSkillUpdateRequestValidator()
    {
        RuleFor(x => x.SkillIds)
            .NotNull();
    }
}
