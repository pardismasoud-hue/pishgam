using Domain.Common;

namespace Domain.Entities;

public class ExpertProfile : AuditableEntity
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
}
