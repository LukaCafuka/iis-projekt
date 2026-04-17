using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IIS.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace IIS.Api.Services;

public class JwtTokenService(IConfiguration configuration)
{
    private const string AccessMinutesKey = "Jwt:AccessTokenMinutes";
    private const string RefreshDaysKey = "Jwt:RefreshTokenDays";

    public (string Token, DateTimeOffset Expires) CreateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? ""),
            new(ClaimTypes.NameIdentifier, user.Id)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var minutes = int.TryParse(configuration[AccessMinutesKey], out var m) ? m : 30;
        var expires = DateTimeOffset.UtcNow.AddMinutes(minutes);
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expires.UtcDateTime,
            signingCredentials: creds);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public static string CreateOpaqueRefreshToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public static string HashRefreshToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hash);
    }

    public DateTimeOffset GetRefreshExpiry() =>
        DateTimeOffset.UtcNow.AddDays(int.TryParse(configuration[RefreshDaysKey], out var d) ? d : 7);
}
