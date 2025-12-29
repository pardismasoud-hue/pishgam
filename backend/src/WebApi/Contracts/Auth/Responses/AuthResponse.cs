namespace WebApi.Contracts.Auth.Responses;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
