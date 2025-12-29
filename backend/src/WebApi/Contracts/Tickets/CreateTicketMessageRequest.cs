namespace WebApi.Contracts.Tickets;

public class CreateTicketMessageRequest
{
    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
}
