using FluentValidation;
using WebApi.Contracts.Tickets;

namespace WebApi.Validation.Tickets;

public class CreateTicketTimeLogRequestValidator : AbstractValidator<CreateTicketTimeLogRequest>
{
    public CreateTicketTimeLogRequestValidator()
    {
        RuleFor(x => x.Minutes)
            .GreaterThan(0);

        RuleFor(x => x.WorkType)
            .IsInEnum();
    }
}
