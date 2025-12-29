using Domain.Common;

namespace Domain.Entities;

public class ContractAsset : AuditableEntity
{
    public Guid ContractId { get; set; }
    public Guid AssetId { get; set; }
}
