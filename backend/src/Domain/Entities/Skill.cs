using Domain.Common;

namespace Domain.Entities;

public class Skill : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
