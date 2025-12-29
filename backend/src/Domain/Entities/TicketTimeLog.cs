using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class TicketTimeLog : AuditableEntity
{
    public Guid TicketId { get; set; }
    public Guid ExpertProfileId { get; set; }
    public int Minutes { get; set; }
    public TicketWorkType WorkType { get; set; }
    public DateTime LoggedAtUtc { get; set; }
}
