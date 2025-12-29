namespace WebApi.Contracts.AdminUsers;

public class AdminUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    public DateTime CreatedAtUtc { get; set; }
    public bool IsLockedOut { get; set; }
    public bool EmailConfirmed { get; set; }
}
