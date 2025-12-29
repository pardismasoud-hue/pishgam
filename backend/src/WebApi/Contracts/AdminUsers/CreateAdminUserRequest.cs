namespace WebApi.Contracts.AdminUsers;

public class CreateAdminUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? CompanyName { get; set; }
}
