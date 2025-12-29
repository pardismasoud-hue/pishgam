using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class TicketMessage : AuditableEntity
{
    public Guid TicketId { get; set; }
    public string AuthorUserId { get; set; } = string.Empty;
    public TicketMessageAuthorRole AuthorRole { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
}
