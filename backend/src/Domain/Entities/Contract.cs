using Domain.Common;

namespace Domain.Entities;

public class Contract : AuditableEntity
{
    public Guid CompanyProfileId { get; set; }
    public decimal MonthlyPrice { get; set; }
    public int IncludedSupportMinutesPerMonth { get; set; }
    public int OnsiteDaysIncluded { get; set; }
    public bool IsActive { get; set; }
}
