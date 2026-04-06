using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IIS.Client.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace IIS.Client.Services;

public class AuthHeaderHandler(
    AuthTokenStore tokens,
    BrowserTokenStorage browserStorage,
    IHttpClientFactory httpFactory,
    AuthenticationStateProvider authState) : DelegatingHandler
{
    private static readonly TimeSpan RefreshAhead = TimeSpan.FromMinutes(1);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? "";
        var isAuthEndpoint = IsAuthEndpoint(path);
        HttpRequestMessage? retryRequest = null;

        if (!isAuthEndpoint)
        {
            retryRequest = await CloneRequestAsync(request, cancellationToken).ConfigureAwait(false);

            if (IsTokenExpiringSoon(tokens.AccessToken))
                await TryRefreshTokensAsync(tokens.AccessToken, cancellationToken).ConfigureAwait(false);

            AttachBearer(request, tokens.AccessToken);
        }

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (isAuthEndpoint || response.StatusCode != HttpStatusCode.Unauthorized || retryRequest == null)
        {
            retryRequest?.Dispose();
            return response;
        }

        var refreshed = await TryRefreshTokensAsync(tokens.AccessToken, cancellationToken).ConfigureAwait(false);
        if (!refreshed)
        {
            retryRequest.Dispose();
            return response;
        }

        response.Dispose();
        AttachBearer(retryRequest, tokens.AccessToken);
        return await base.SendAsync(retryRequest, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsAuthEndpoint(string path) =>
        path.Contains("/api/auth/login", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/api/auth/register", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/api/auth/refresh", StringComparison.OrdinalIgnoreCase);

    private static void AttachBearer(HttpRequestMessage request, string? accessToken)
    {
        request.Headers.Authorization = string.IsNullOrWhiteSpace(accessToken)
            ? null
            : new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private static bool IsTokenExpiringSoon(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return false;

        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            return jwt.ValidTo <= DateTime.UtcNow.Add(RefreshAhead);
        }
        catch
        {
            return true;
        }
    }

    private async Task<bool> TryRefreshTokensAsync(string? tokenUsedByRequest, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tokens.RefreshToken))
            return false;

        await tokens.RefreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!string.IsNullOrWhiteSpace(tokens.AccessToken)
                && !string.Equals(tokens.AccessToken, tokenUsedByRequest, StringComparison.Ordinal))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(tokens.RefreshToken))
                return false;

            var client = httpFactory.CreateClient("ApiNoAuth");
            using var response = await client.PostAsJsonAsync(
                "api/auth/refresh",
                new RefreshRequest { RefreshToken = tokens.RefreshToken },
                cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                await ClearAuthStateAsync().ConfigureAwait(false);
                return false;
            }

            var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (payload == null || string.IsNullOrWhiteSpace(payload.AccessToken) || string.IsNullOrWhiteSpace(payload.RefreshToken))
            {
                await ClearAuthStateAsync().ConfigureAwait(false);
                return false;
            }

            tokens.AccessToken = payload.AccessToken;
            tokens.RefreshToken = payload.RefreshToken;
            await browserStorage.SaveAsync(payload.AccessToken, payload.RefreshToken).ConfigureAwait(false);
            NotifyAuthStateChanged();
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            await ClearAuthStateAsync().ConfigureAwait(false);
            return false;
        }
        finally
        {
            tokens.RefreshLock.Release();
        }
    }

    private async Task ClearAuthStateAsync()
    {
        tokens.Clear();
        await browserStorage.ClearAsync().ConfigureAwait(false);
        NotifyAuthStateChanged();
    }

    private void NotifyAuthStateChanged()
    {
        if (authState is JwtAuthStateProvider jwtAuth)
            jwtAuth.NotifyChanged();
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (request.Content != null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            var content = new ByteArrayContent(bytes);
            foreach (var header in request.Content.Headers)
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            clone.Content = content;
        }

        return clone;
    }
}
