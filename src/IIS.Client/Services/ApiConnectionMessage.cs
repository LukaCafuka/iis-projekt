namespace IIS.Client.Services;

public static class ApiConnectionMessage
{
    public static string FromException(Exception ex, string baseUrl) =>
        ex is HttpRequestException
            ? $"Cannot reach the API at {baseUrl}. Start the backend first: dotnet run --project src/IIS.Api"
            : ex.Message;
}
