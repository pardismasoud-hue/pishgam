namespace WebApi.Contracts.Kpi;

public class ExpertKpiDto
{
    public Guid ExpertProfileId { get; set; }
    public int TotalAssignedTickets { get; set; }
    public int TotalResolvedTickets { get; set; }
    public double? AverageFirstResponseMinutes { get; set; }
    public double? AverageResolutionMinutes { get; set; }
    public int FirstResponseBreachCount { get; set; }
    public int ResolutionBreachCount { get; set; }
    public int SatisfactionCount { get; set; }
    public double? AverageRating { get; set; }
    public double? AverageResponseTimeRating { get; set; }
    public double? AverageResolutionQualityRating { get; set; }
    public double? AverageCommunicationRating { get; set; }
    public int TotalLoggedMinutes { get; set; }
}
