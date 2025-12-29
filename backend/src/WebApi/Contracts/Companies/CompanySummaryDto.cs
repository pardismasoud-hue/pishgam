namespace WebApi.Contracts.Companies;

public class CompanySummaryDto
{
    public string UserId { get; set; } = string.Empty;
    public Guid CompanyProfileId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
