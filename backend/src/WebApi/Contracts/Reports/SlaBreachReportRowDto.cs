namespace WebApi.Contracts.Reports;

public class SlaBreachReportRowDto
{
    public Guid TicketId { get; set; }
    public string TicketTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? ServiceName { get; set; }
    public string BreachType { get; set; } = string.Empty;
    public DateTime? BreachedAtUtc { get; set; }
}
