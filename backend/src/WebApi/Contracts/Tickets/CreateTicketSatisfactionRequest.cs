namespace WebApi.Contracts.Tickets;

public class CreateTicketSatisfactionRequest
{
    public int Rating { get; set; }
    public int? ResponseTimeRating { get; set; }
    public int? ResolutionQualityRating { get; set; }
    public int? CommunicationRating { get; set; }
    public string? Comment { get; set; }
}
