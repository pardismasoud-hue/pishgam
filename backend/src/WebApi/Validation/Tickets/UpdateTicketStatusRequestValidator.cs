using FluentValidation;
using WebApi.Contracts.Tickets;

namespace WebApi.Validation.Tickets;

public class UpdateTicketStatusRequestValidator : AbstractValidator<UpdateTicketStatusRequest>
{
    public UpdateTicketStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum();
    }
}
