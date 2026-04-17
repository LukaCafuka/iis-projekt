using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace IIS.Client.Services;

public class JwtAuthStateProvider(AuthTokenStore store) : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (string.IsNullOrEmpty(store.AccessToken))
            return Task.FromResult(Anonymous());

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(store.AccessToken);
            if (jwt.ValidTo <= DateTime.UtcNow)
                return Task.FromResult(Anonymous());

            var claims = jwt.Claims.ToList();
            if (claims.All(c => c.Type != ClaimTypes.Name))
            {
                var name = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.UniqueName)?.Value
                    ?? claims.FirstOrDefault(c => c.Type == "unique_name")?.Value
                    ?? claims.FirstOrDefault(c => c.Type == "name")?.Value;
                if (!string.IsNullOrEmpty(name))
                    claims.Add(new Claim(ClaimTypes.Name, name));
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }
        catch
        {
            return Task.FromResult(Anonymous());
        }
    }

    public void NotifyChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    private static AuthenticationState Anonymous() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));
}
