using Domain.Enums;

namespace WebApi.Contracts.Tickets;

public class TicketDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public Guid CompanyProfileId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid? ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public Guid? AssetId { get; set; }
    public string? AssetName { get; set; }
    public Guid? AssignedExpertProfileId { get; set; }
    public string? AssignedExpertName { get; set; }
    public int SlaFirstResponseMinutes { get; set; }
    public int SlaResolutionMinutes { get; set; }
    public DateTime FirstResponseDueAtUtc { get; set; }
    public DateTime ResolutionDueAtUtc { get; set; }
    public DateTime? FirstResponseAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public bool FirstResponseBreached { get; set; }
    public bool ResolutionBreached { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
