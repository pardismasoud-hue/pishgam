namespace WebApi.Contracts.Contracts;

public class CreateContractRequest
{
    public string CompanyUserId { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public int IncludedSupportMinutesPerMonth { get; set; }
    public int OnsiteDaysIncluded { get; set; }
    public bool IsActive { get; set; }
    public List<ContractServiceRequest> Services { get; set; } = new();
    public List<Guid> AssetIds { get; set; } = new();
}
