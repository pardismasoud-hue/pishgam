using FluentValidation;
using WebApi.Contracts.Tickets;

namespace WebApi.Validation.Tickets;

public class CreateTicketSatisfactionRequestValidator : AbstractValidator<CreateTicketSatisfactionRequest>
{
    public CreateTicketSatisfactionRequestValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5);

        RuleFor(x => x.ResponseTimeRating)
            .InclusiveBetween(1, 5)
            .When(x => x.ResponseTimeRating.HasValue);

        RuleFor(x => x.ResolutionQualityRating)
            .InclusiveBetween(1, 5)
            .When(x => x.ResolutionQualityRating.HasValue);

        RuleFor(x => x.CommunicationRating)
            .InclusiveBetween(1, 5)
            .When(x => x.CommunicationRating.HasValue);

        RuleFor(x => x.Comment)
            .MaximumLength(2000);
    }
}
