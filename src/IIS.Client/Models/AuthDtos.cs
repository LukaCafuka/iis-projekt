using System.ComponentModel.DataAnnotations;

namespace IIS.Client.Models;

public class LoginRequest
{
    [Required] public string Username { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public class RegisterRequest
{
    [Required, MinLength(3)] public string Username { get; set; } = "";
    [Required, MinLength(6)] public string Password { get; set; } = "";
}

public class TokenResponse
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
}

public class RefreshRequest
{
    [Required] public string RefreshToken { get; set; } = "";
}
