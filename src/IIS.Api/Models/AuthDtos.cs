namespace IIS.Api.Models;

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset AccessTokenExpires { get; set; }
}

public class RefreshRequest
{
    /// <summary>Optional when refresh token is sent via HttpOnly cookie.</summary>
    public string? RefreshToken { get; set; }
}
