namespace WebApi.Contracts.Tickets;

public class ClosedTicketSatisfactionDto
{
    public Guid TicketId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ServiceName { get; set; }
    public string? AssetName { get; set; }
    public DateTime ClosedAtUtc { get; set; }
    public bool SatisfactionSubmitted { get; set; }
    public int? Rating { get; set; }
    public int? ResponseTimeRating { get; set; }
    public int? ResolutionQualityRating { get; set; }
    public int? CommunicationRating { get; set; }
    public string? Comment { get; set; }
}
