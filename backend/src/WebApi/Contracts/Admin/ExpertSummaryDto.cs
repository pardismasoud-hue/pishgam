namespace WebApi.Contracts.Admin;

public class ExpertSummaryDto
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
