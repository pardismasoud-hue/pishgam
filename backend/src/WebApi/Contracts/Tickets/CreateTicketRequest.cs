namespace WebApi.Contracts.Tickets;

public class CreateTicketRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? ServiceId { get; set; }
    public Guid? AssetId { get; set; }
}
