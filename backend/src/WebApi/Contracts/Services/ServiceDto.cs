namespace WebApi.Contracts.Services;

public class ServiceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DefaultFirstResponseMinutes { get; set; }
    public int DefaultResolutionMinutes { get; set; }
    public bool IsActive { get; set; }
}
