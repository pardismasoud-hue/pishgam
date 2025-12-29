namespace WebApi.Contracts.Contracts;

public class ContractDto
{
    public Guid Id { get; set; }
    public Guid CompanyProfileId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public int IncludedSupportMinutesPerMonth { get; set; }
    public int OnsiteDaysIncluded { get; set; }
    public bool IsActive { get; set; }
    public List<ContractServiceDto> Services { get; set; } = new();
    public List<ContractAssetDto> Assets { get; set; } = new();
}
