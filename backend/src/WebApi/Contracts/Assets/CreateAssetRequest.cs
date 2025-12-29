namespace WebApi.Contracts.Assets;

public class CreateAssetRequest
{
    public string Name { get; set; } = string.Empty;
    public string? AssetTag { get; set; }
    public string? SerialNumber { get; set; }
    public string? Notes { get; set; }
}
