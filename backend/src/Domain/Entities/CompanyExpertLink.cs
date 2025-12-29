using Domain.Common;

namespace Domain.Entities;

public class CompanyExpertLink : AuditableEntity
{
    public Guid CompanyProfileId { get; set; }
    public Guid ExpertProfileId { get; set; }
    public bool IsPrimary { get; set; }
}
