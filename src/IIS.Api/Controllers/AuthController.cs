using System.Security.Claims;
using IIS.Api.Data;
using IIS.Api.Entities;
using IIS.Api.Models;
using IIS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IIS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly ApplicationDbContext _db;
    private readonly JwtTokenService _jwt;

    public AuthController(UserManager<ApplicationUser> users, ApplicationDbContext db, JwtTokenService jwt)
    {
        _users = users;
        _db = db;
        _jwt = jwt;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var user = new ApplicationUser { UserName = request.Username };
        var result = await _users.CreateAsync(user, request.Password).ConfigureAwait(false);
        if (!result.Succeeded)
            return BadRequest(string.Join("; ", result.Errors.Select(e => e.Description)));

        await _users.AddToRoleAsync(user, "Reader").ConfigureAwait(false);
        return await IssueTokensAsync(user, ct).ConfigureAwait(false);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await _users.FindByNameAsync(request.Username).ConfigureAwait(false);
        if (user == null)
            return Unauthorized();
        if (!await _users.CheckPasswordAsync(user, request.Password).ConfigureAwait(false))
            return Unauthorized();

        return await IssueTokensAsync(user, ct).ConfigureAwait(false);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var hash = JwtTokenService.HashRefreshToken(request.RefreshToken);
        var existing = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
            .ConfigureAwait(false);
        if (existing == null || existing.ExpiresAt < DateTimeOffset.UtcNow)
            return Unauthorized();

        _db.RefreshTokens.Remove(existing);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return await IssueTokensAsync(existing.User, ct).ConfigureAwait(false);
    }

    private async Task<TokenResponse> IssueTokensAsync(ApplicationUser user, CancellationToken ct)
    {
        var roles = await _users.GetRolesAsync(user).ConfigureAwait(false);
        var (access, exp) = _jwt.CreateAccessToken(user, roles);
        var refresh = JwtTokenService.CreateOpaqueRefreshToken();
        var refreshHash = JwtTokenService.HashRefreshToken(refresh);
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = _jwt.GetRefreshExpiry()
        });
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return new TokenResponse
        {
            AccessToken = access,
            RefreshToken = refresh,
            AccessTokenExpires = exp
        };
    }
}
