namespace WebApi.Contracts.Admin;

public class CompanyExpertDto
{
    public Guid LinkId { get; set; }
    public string ExpertUserId { get; set; } = string.Empty;
    public Guid ExpertProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public bool IsPrimary { get; set; }
}
