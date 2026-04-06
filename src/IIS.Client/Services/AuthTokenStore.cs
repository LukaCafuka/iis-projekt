namespace IIS.Client.Services;

public class AuthTokenStore
{
    public SemaphoreSlim RefreshLock { get; } = new(1, 1);

    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }

    public void Clear()
    {
        AccessToken = null;
        RefreshToken = null;
    }
}
