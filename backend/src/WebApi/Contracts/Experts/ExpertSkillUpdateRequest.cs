namespace WebApi.Contracts.Experts;

public class ExpertSkillUpdateRequest
{
    public IReadOnlyList<Guid> SkillIds { get; set; } = Array.Empty<Guid>();
}
