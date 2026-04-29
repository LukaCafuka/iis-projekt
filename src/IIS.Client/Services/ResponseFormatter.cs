using System.Text.Json;
using System.Xml.Linq;

namespace IIS.Client.Services;

public static class ResponseFormatter
{
    private static readonly JsonSerializerOptions IndentedJson = new()
    {
        WriteIndented = true
    };

    public static string FormatHttpResponse(HttpResponseMessage response, string body)
    {
        var status = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        var payload = PrettyPayload(body, mediaType);
        return string.IsNullOrEmpty(payload)
            ? status
            : $"{status}{Environment.NewLine}{Environment.NewLine}{payload}";
    }

    public static string FormatException(Exception ex) =>
        $"{ex.GetType().Name}: {ex.Message}";

    private static string PrettyPayload(string text, string? mediaType)
    {
        var trimmed = text?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return string.Empty;

        if (IsJson(mediaType, trimmed))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                return JsonSerializer.Serialize(doc.RootElement, IndentedJson);
            }
            catch
            {
                // Return raw payload when formatting fails.
            }
        }

        if (IsXml(mediaType, trimmed))
        {
            try
            {
                var doc = XDocument.Parse(trimmed);
                var declaration = doc.Declaration == null ? "" : doc.Declaration + Environment.NewLine;
                return declaration + doc.ToString();
            }
            catch
            {
                // Return raw payload when formatting fails.
            }
        }

        return trimmed;
    }

    private static bool IsJson(string? mediaType, string payload)
    {
        if (!string.IsNullOrEmpty(mediaType)
            && (mediaType.Contains("json", StringComparison.OrdinalIgnoreCase)
                || mediaType.Contains("+json", StringComparison.OrdinalIgnoreCase)))
            return true;

        return payload.StartsWith("{", StringComparison.Ordinal)
            || payload.StartsWith("[", StringComparison.Ordinal);
    }

    private static bool IsXml(string? mediaType, string payload)
    {
        if (!string.IsNullOrEmpty(mediaType)
            && (mediaType.Contains("xml", StringComparison.OrdinalIgnoreCase)
                || mediaType.Contains("+xml", StringComparison.OrdinalIgnoreCase)))
            return true;

        return payload.StartsWith("<", StringComparison.Ordinal);
    }
}