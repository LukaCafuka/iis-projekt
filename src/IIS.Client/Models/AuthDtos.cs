using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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
    /// <summary>Must match API camelCase JSON so JWT values deserialize.</summary>
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = "";
}


public class RefreshRequest
{
    public string? RefreshToken { get; set; }
}
