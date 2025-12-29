using Domain.Enums;

namespace WebApi.Services;

public static class TicketWorkflow
{
    public static bool IsValidTransition(TicketStatus current, TicketStatus next)
    {
        return current switch
        {
            TicketStatus.Open => next is TicketStatus.InProgress,
            TicketStatus.InProgress => next is TicketStatus.WaitingForCustomer or TicketStatus.Resolved,
            TicketStatus.WaitingForCustomer => next is TicketStatus.InProgress,
            TicketStatus.Resolved => next is TicketStatus.Closed,
            TicketStatus.Closed => false,
            _ => false
        };
    }
}
