using Domain.Common;

namespace Domain.Entities;

public class TicketSatisfaction : AuditableEntity
{
    public Guid TicketId { get; set; }
    public Guid CompanyProfileId { get; set; }
    public int Rating { get; set; }
    public int? ResponseTimeRating { get; set; }
    public int? ResolutionQualityRating { get; set; }
    public int? CommunicationRating { get; set; }
    public string? Comment { get; set; }
}
