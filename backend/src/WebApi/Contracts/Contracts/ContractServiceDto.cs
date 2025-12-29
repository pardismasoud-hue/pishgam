namespace WebApi.Contracts.Contracts;

public class ContractServiceDto
{
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int DefaultFirstResponseMinutes { get; set; }
    public int DefaultResolutionMinutes { get; set; }
    public int? CustomFirstResponseMinutes { get; set; }
    public int? CustomResolutionMinutes { get; set; }
}
