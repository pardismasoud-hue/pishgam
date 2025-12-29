using Domain.Common;

namespace Domain.Entities;

public class ContractService : AuditableEntity
{
    public Guid ContractId { get; set; }
    public Guid ServiceCatalogItemId { get; set; }
    public int? CustomFirstResponseMinutes { get; set; }
    public int? CustomResolutionMinutes { get; set; }
}
