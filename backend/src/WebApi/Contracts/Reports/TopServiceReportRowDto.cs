namespace WebApi.Contracts.Reports;

public class TopServiceReportRowDto
{
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int TicketCount { get; set; }
    public double? AverageResolutionMinutes { get; set; }
    public int BreachCount { get; set; }
}
