using Domain.Common;

namespace Domain.Entities;

public class Asset : AuditableEntity
{
    public Guid CompanyProfileId { get; set; }
    public Guid? PrimaryExpertProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AssetTag { get; set; }
    public string? SerialNumber { get; set; }
    public string? Notes { get; set; }
}
