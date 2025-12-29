using Domain.Common;

namespace Domain.Entities;

public class ExpertSkill : AuditableEntity
{
    public Guid ExpertProfileId { get; set; }
    public Guid SkillId { get; set; }
}
