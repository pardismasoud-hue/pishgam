namespace WebApi.Contracts.Skills;

public class UpdateSkillRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
