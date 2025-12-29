using Domain.Common;

namespace Domain.Entities;

public class CompanyProfile : AuditableEntity
{
    public string UserId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
}
