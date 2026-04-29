using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace IIS.Client.Services;

public static class BrowserTokenStorageKeys
{
    public const string AccessToken = "iis.accessToken";
    public const string RefreshToken = "iis.refreshToken";
}

public class BrowserTokenStorage(ProtectedSessionStorage session)
{
    public async ValueTask SaveAsync(string? accessToken, string? refreshToken)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            await session.DeleteAsync(BrowserTokenStorageKeys.AccessToken);
            await session.DeleteAsync(BrowserTokenStorageKeys.RefreshToken);
            return;
        }

        await session.SetAsync(BrowserTokenStorageKeys.AccessToken, accessToken);
        if (!string.IsNullOrEmpty(refreshToken))
            await session.SetAsync(BrowserTokenStorageKeys.RefreshToken, refreshToken);
    }

    public async ValueTask<(string? Access, string? Refresh)> LoadAsync()
    {
        try
        {
            var a = await session.GetAsync<string>(BrowserTokenStorageKeys.AccessToken);
            var r = await session.GetAsync<string>(BrowserTokenStorageKeys.RefreshToken);
            var access = a.Success ? a.Value : null;
            var refresh = r.Success ? r.Value : null;
            return (access, refresh);
        }
        catch
        {
            return (null, null);
        }
    }

    public async ValueTask ClearAsync() => await SaveAsync(null, null);
}
