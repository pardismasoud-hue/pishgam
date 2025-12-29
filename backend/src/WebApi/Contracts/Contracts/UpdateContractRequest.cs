namespace WebApi.Contracts.Contracts;

public class UpdateContractRequest
{
    public decimal MonthlyPrice { get; set; }
    public int IncludedSupportMinutesPerMonth { get; set; }
    public int OnsiteDaysIncluded { get; set; }
    public bool IsActive { get; set; }
    public List<ContractServiceRequest> Services { get; set; } = new();
    public List<Guid> AssetIds { get; set; } = new();
}
