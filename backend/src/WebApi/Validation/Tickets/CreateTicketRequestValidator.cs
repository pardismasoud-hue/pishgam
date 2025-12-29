using FluentValidation;
using WebApi.Contracts.Tickets;

namespace WebApi.Validation.Tickets;

public class CreateTicketRequestValidator : AbstractValidator<CreateTicketRequest>
{
    public CreateTicketRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(4000);

        RuleFor(x => x)
            .Must(x => x.ServiceId.HasValue || x.AssetId.HasValue)
            .WithMessage("ServiceId or AssetId is required.");
    }
}
