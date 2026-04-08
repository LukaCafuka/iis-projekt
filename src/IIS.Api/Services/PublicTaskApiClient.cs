using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using IIS.Api.Models;
using IIS.Api.Options;
using Microsoft.Extensions.Options;

namespace IIS.Api.Services;

public class PublicTaskApiClient
{
    private readonly HttpClient _http;
    private readonly TaskApiOptions _options;
    private static readonly JsonSerializerOptions WriteJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
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
        var response = await _http.GetAsync("tasks", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await ReadTaskListAsync(response, ct).ConfigureAwait(false);
    }

    public async Task<TaskDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"tasks/{Uri.EscapeDataString(id)}", ct).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await ReadTaskAsync(response, ct).ConfigureAwait(false);
    }

    public async Task<TaskDto> CreateAsync(TaskDto dto, CancellationToken ct = default)
    {
        var payload = new
        {
            name = dto.Name,
            description = dto.Description,
            completed = dto.Completed,
            createdAt = dto.CreatedAt
        };

        var response = await _http.PostAsJsonAsync("tasks", payload, WriteJsonOptions, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var created = await ReadTaskAsync(response, ct).ConfigureAwait(false) ?? new TaskDto();
        if (string.IsNullOrWhiteSpace(created.Id))
            created.Id = TryGetIdFromLocationHeader(response.Headers.Location);

        return created;
    }

    public async Task<TaskDto?> UpdateAsync(string id, TaskDto dto, CancellationToken ct = default)
    {
        var payload = new
        {
            name = dto.Name,
            description = dto.Description,
            completed = dto.Completed,
            createdAt = dto.CreatedAt
        };

        var response = await _http.PutAsJsonAsync($"tasks/{Uri.EscapeDataString(id)}", payload, WriteJsonOptions, ct).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await ReadTaskAsync(response, ct).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"tasks/{Uri.EscapeDataString(id)}", ct).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return false;
        response.EnsureSuccessStatusCode();
        return true;
    }

    private static async Task<IReadOnlyList<TaskDto>> ReadTaskListAsync(HttpResponseMessage response, CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

        var list = new List<TaskDto>();
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return list;

        foreach (var el in doc.RootElement.EnumerateArray())
        {
            if (el.ValueKind == JsonValueKind.Object)
                list.Add(ParseTask(el));
        }

        return list;
    }

    private static async Task<TaskDto?> ReadTaskAsync(HttpResponseMessage response, CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            return null;

        return ParseTask(doc.RootElement);
    }

    private static TaskDto ParseTask(JsonElement el)
    {
        return new TaskDto
        {
            // Prefer canonical lowercase keys from MockAPI over legacy uppercase keys.
            Id = ReadString(el, "id", "Id"),
            Name = ReadString(el, "name", "Name") ?? string.Empty,
            Description = ReadString(el, "description", "Description") ?? string.Empty,
            Completed = ReadBool(el, "completed", "Completed"),
            CreatedAt = ReadDateTimeOffset(el, "createdAt", "CreatedAt")
        };
    }

    private static string? ReadString(JsonElement el, string primary, string fallback)
    {
        if (el.TryGetProperty(primary, out var p))
            return JsonValueToString(p);

        if (el.TryGetProperty(fallback, out var f))
            return JsonValueToString(f);

        return null;
    }

    private static bool ReadBool(JsonElement el, string primary, string fallback)
    {
        if (TryReadBool(el, primary, out var value))
            return value;

        return TryReadBool(el, fallback, out value) && value;
    }

    private static bool TryReadBool(JsonElement el, string name, out bool value)
    {
        value = false;
        if (!el.TryGetProperty(name, out var p))
            return false;

        switch (p.ValueKind)
        {
            case JsonValueKind.True:
                value = true;
                return true;
            case JsonValueKind.False:
                value = false;
                return true;
            case JsonValueKind.String:
                return bool.TryParse(p.GetString(), out value);
            case JsonValueKind.Number:
                if (p.TryGetInt32(out var n))
                {
                    value = n != 0;
                    return true;
                }

                return false;
            default:
                return false;
        }
    }

    private static DateTimeOffset? ReadDateTimeOffset(JsonElement el, string primary, string fallback)
    {
        if (TryReadDateTimeOffset(el, primary, out var value))
            return value;

        return TryReadDateTimeOffset(el, fallback, out value) ? value : null;
    }

    private static bool TryReadDateTimeOffset(JsonElement el, string name, out DateTimeOffset value)
    {
        value = default;
        if (!el.TryGetProperty(name, out var p))
            return false;

        if (p.ValueKind == JsonValueKind.String)
            return DateTimeOffset.TryParse(p.GetString(), out value);

        return false;
    }

    private static string? JsonValueToString(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => value.ToString()
        };
    }

    private static string? TryGetIdFromLocationHeader(Uri? location)
    {
        if (location == null)
            return null;

        var segments = location.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
            return null;

        return Uri.UnescapeDataString(segments[^1]);
    }
}
