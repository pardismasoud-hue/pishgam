using FluentValidation;
using WebApi.Contracts.Tickets;

namespace WebApi.Validation.Tickets;

public class CreateTicketMessageRequestValidator : AbstractValidator<CreateTicketMessageRequest>
{
    public CreateTicketMessageRequestValidator()
    {
        RuleFor(x => x.Body)
            .NotEmpty()
            .MaximumLength(4000);
    }
}
