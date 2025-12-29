namespace WebApi.Contracts.Reports;

public class ExpertPerformanceReportRowDto
{
    public Guid ExpertProfileId { get; set; }
    public string ExpertName { get; set; } = string.Empty;
    public int AssignedTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public double? AverageFirstResponseMinutes { get; set; }
    public double? AverageResolutionMinutes { get; set; }
    public int FirstResponseBreaches { get; set; }
    public int ResolutionBreaches { get; set; }
    public double? AverageRating { get; set; }
}
