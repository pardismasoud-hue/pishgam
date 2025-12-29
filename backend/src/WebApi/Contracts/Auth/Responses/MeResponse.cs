namespace WebApi.Contracts.Auth.Responses;

public class MeResponse
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool? ExpertApproved { get; set; }
    public string? FullName { get; set; }
    public string? CompanyName { get; set; }
}
