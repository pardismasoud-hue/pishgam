namespace WebApi.Contracts.Contracts;

public class ContractAssetDto
{
    public Guid AssetId { get; set; }
    public string AssetName { get; set; } = string.Empty;
    public string? AssetTag { get; set; }
    public string? SerialNumber { get; set; }
}
