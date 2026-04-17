using System.Net.Http.Json;
using System.Text.Json;
using IIS.Api.Models;
using IIS.Api.Options;
using Microsoft.Extensions.Options;

namespace IIS.Api.Services;

public class PublicTaskApiClient
{
    private readonly HttpClient _http;
    private readonly TaskApiOptions _options;

    // mockapi.io uses camelCase for its native fields (id, name, description,
    // completed, createdAt). Serialising with this policy means we both send
    // and match the lower-case names only, so we don't pollute mockapi records
    // with parallel PascalCase keys and don't accidentally pick up any stray
    // "Id": null written by earlier requests.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PublicTaskApiClient(HttpClient http, IOptions<TaskApiOptions> options)
    {
        _http = http;
        _options = options.Value;
        if (!string.IsNullOrEmpty(_options.PublicBaseUrl))
            _http.BaseAddress = new Uri(_options.PublicBaseUrl.TrimEnd('/') + "/");
    }

    public async Task<IReadOnlyList<TaskDto>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await _http.GetFromJsonAsync<List<TaskDto>>("tasks", JsonOptions, ct).ConfigureAwait(false);
        return list ?? new List<TaskDto>();
    }

    public async Task<TaskDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<TaskDto>($"tasks/{Uri.EscapeDataString(id)}", JsonOptions, ct).ConfigureAwait(false);
    }

    public async Task<TaskDto> CreateAsync(TaskDto dto, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("tasks", dto, JsonOptions, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskDto>(JsonOptions, ct).ConfigureAwait(false))!;
    }

    public async Task<TaskDto?> UpdateAsync(string id, TaskDto dto, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"tasks/{Uri.EscapeDataString(id)}", dto, JsonOptions, ct).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TaskDto>(JsonOptions, ct).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"tasks/{Uri.EscapeDataString(id)}", ct).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return false;
        response.EnsureSuccessStatusCode();
        return true;
    }
}
