namespace WebApi.Contracts.AdminUsers;

public class UpdateUserRoleRequest
{
    public string Role { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? CompanyName { get; set; }
}
