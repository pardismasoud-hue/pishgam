namespace WebApi.Contracts.Contracts;

public class ContractServiceRequest
{
    public Guid ServiceId { get; set; }
    public int? CustomFirstResponseMinutes { get; set; }
    public int? CustomResolutionMinutes { get; set; }
}
