namespace WebApi.Contracts.Assets;

public class AssetDto
{
    public Guid Id { get; set; }
    public Guid CompanyProfileId { get; set; }
    public string? CompanyName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AssetTag { get; set; }
    public string? SerialNumber { get; set; }
    public string? Notes { get; set; }
}
