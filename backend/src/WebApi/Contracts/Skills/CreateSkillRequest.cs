namespace WebApi.Contracts.Skills;

public class CreateSkillRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
