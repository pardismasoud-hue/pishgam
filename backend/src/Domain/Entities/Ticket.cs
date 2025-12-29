using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Ticket : AuditableEntity
{
    public Guid CompanyProfileId { get; set; }
    public Guid? ServiceCatalogItemId { get; set; }
    public Guid? AssetId { get; set; }
    public Guid? AssignedExpertProfileId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public int SlaFirstResponseMinutes { get; set; }
    public int SlaResolutionMinutes { get; set; }
    public DateTime FirstResponseDueAtUtc { get; set; }
    public DateTime ResolutionDueAtUtc { get; set; }
    public DateTime? FirstResponseAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public bool FirstResponseBreached { get; set; }
    public bool ResolutionBreached { get; set; }
}
