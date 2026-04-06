using Grpc.Net.Client;
using IIS.Client.Services;
using IIS.Contracts;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

// gRPC over HTTP (no TLS) requires HTTP/2 cleartext; must run before any HttpClient/gRPC usage.
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddDataProtection();
builder.Services.AddScoped<ProtectedSessionStorage>();
builder.Services.AddScoped<BrowserTokenStorage>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthStateProvider>();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthTokenStore>();
builder.Services.AddScoped<ApiClientWithAuthFactory>();
builder.Services.AddScoped<SoapSearchClient>();
builder.Services.AddHttpClient("ApiNoAuth", (sp, client) =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5136";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
});
builder.Services.AddHttpClient("Api", (sp, client) =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5136";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
});

builder.Services.AddSingleton(sp =>
{
    var url = builder.Configuration["Api:GrpcUrl"] ?? "http://localhost:5137";
    return GrpcChannel.ForAddress(url, new GrpcChannelOptions
    {
        HttpHandler = new SocketsHttpHandler
        {
            EnableMultipleHttp2Connections = true
        }
    });
});
builder.Services.AddSingleton(sp =>
{
    var channel = sp.GetRequiredService<GrpcChannel>();
    return new IIS.Contracts.Weather.WeatherClient(channel);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
