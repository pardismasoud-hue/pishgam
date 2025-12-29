using Domain.Enums;

namespace WebApi.Contracts.Tickets;

public class UpdateTicketStatusRequest
{
    public TicketStatus Status { get; set; }
}
