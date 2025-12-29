namespace WebApi.Contracts.Assets;

public class UpdateAssetRequest
{
    public string Name { get; set; } = string.Empty;
    public string? AssetTag { get; set; }
    public string? SerialNumber { get; set; }
    public string? Notes { get; set; }
}
