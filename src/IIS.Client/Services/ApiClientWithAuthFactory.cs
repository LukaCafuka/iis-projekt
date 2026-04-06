using System.Net.Http.Headers;

namespace IIS.Client.Services;

public class ApiClientWithAuthFactory(IHttpClientFactory httpFactory, AuthTokenStore tokens)
{
    public HttpClient CreateClient()
    {
        var client = httpFactory.CreateClient("Api");
        if (string.IsNullOrWhiteSpace(tokens.AccessToken))
        {
            client.DefaultRequestHeaders.Authorization = null;
        }
        else
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        }

        return client;
    }
}