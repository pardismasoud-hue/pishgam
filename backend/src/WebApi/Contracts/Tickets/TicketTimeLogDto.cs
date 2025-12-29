using Domain.Enums;

namespace WebApi.Contracts.Tickets;

public class TicketTimeLogDto
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Guid ExpertProfileId { get; set; }
    public int Minutes { get; set; }
    public TicketWorkType WorkType { get; set; }
    public DateTime LoggedAtUtc { get; set; }
}
