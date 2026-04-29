using Microsoft.AspNetCore.Http;

namespace IIS.Api.Auth;

public static class AuthCookies
{
    public const string RefreshTokenName = "iis.refresh";

    public static CookieOptions RefreshCookie(bool requestIsHttps)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = requestIsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth",
            IsEssential = true
        };
    }
}
