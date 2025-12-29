using Domain.Enums;

namespace WebApi.Contracts.Tickets;

public class CreateTicketTimeLogRequest
{
    public int Minutes { get; set; }
    public TicketWorkType WorkType { get; set; }
}
