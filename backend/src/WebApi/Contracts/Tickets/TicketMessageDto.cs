using Domain.Enums;

namespace WebApi.Contracts.Tickets;

public class TicketMessageDto
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public string AuthorUserId { get; set; } = string.Empty;
    public TicketMessageAuthorRole AuthorRole { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
