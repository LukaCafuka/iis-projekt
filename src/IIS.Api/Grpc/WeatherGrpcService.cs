using System.Xml.Linq;
using Grpc.Core;
using IIS.Contracts;

namespace IIS.Api.Grpc;

public class WeatherGrpcService(IHttpClientFactory httpFactory, ILogger<WeatherGrpcService> log) : IIS.Contracts.Weather.WeatherBase
{
    private const string DhmzUrl = "https://vrijeme.hr/hrvatska_n.xml";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private static (DateTimeOffset At, XDocument? Doc) s_cache;

    public override async Task<TemperatureReply> GetTemperaturesByCity(CityFilter request, ServerCallContext context)
    {
        var user = context.GetHttpContext().User;
        if (user.Identity?.IsAuthenticated != true)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Authentication required."));
        if (!user.IsInRole("Full"))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Full role required."));

        var part = (request.CityPart ?? "").Trim();
        var reply = new TemperatureReply();
        if (string.IsNullOrEmpty(part))
        {
            reply.ErrorMessage = "CityPart is required.";
            return reply;
        }

        try
        {
            var doc = await LoadDocumentAsync(context.CancellationToken).ConfigureAwait(false);
            if (doc == null)
            {
                reply.ErrorMessage = "Could not load DHMZ XML.";
                return reply;
            }

            var needle = part.ToLowerInvariant();
            foreach (var grad in doc.Descendants().Where(e => e.Name.LocalName.Equals("grad", StringComparison.OrdinalIgnoreCase)))
            {
                var city = FindChildText(grad, "GradIme", "gradime");
                if (string.IsNullOrEmpty(city))
                    continue;
                if (!city.ToLowerInvariant().Contains(needle, StringComparison.Ordinal))
                    continue;

                // DHMZ nests measurements under <Podatci>; temperature is <Temp>, text is <Vrijeme> (not <Opis>).
                var podatci = grad.Elements().FirstOrDefault(e =>
                    e.Name.LocalName.Equals("Podatci", StringComparison.OrdinalIgnoreCase));
                var temp = podatci != null
                    ? FindChildText(podatci, "Temp", "temp")
                    : FindDescendantText(grad, "Temp", "temp");
                var desc = podatci != null
                    ? FindChildText(podatci, "Vrijeme", "vrijeme", "Opis", "opis")
                    : FindDescendantText(grad, "Vrijeme", "vrijeme", "Opis", "opis");

                reply.Entries.Add(new CityTemperature
                {
                    CityName = city,
                    TemperatureC = temp?.Trim() ?? "",
                    Description = desc?.Trim() ?? ""
                });
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "DHMZ fetch failed");
            reply.ErrorMessage = ex.Message;
        }

        return reply;
    }

    private async Task<XDocument?> LoadDocumentAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        if (s_cache.Doc != null && now - s_cache.At < CacheTtl)
            return s_cache.Doc;

        var client = httpFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("IIS-Interop/1.0");
        client.Timeout = TimeSpan.FromSeconds(30);
        await using var stream = await client.GetStreamAsync(new Uri(DhmzUrl), ct).ConfigureAwait(false);
        var doc = await XDocument.LoadAsync(stream, LoadOptions.None, ct).ConfigureAwait(false);
        s_cache = (now, doc);
        return doc;
    }

    private static string? FindChildText(XElement parent, params string[] localNames)
    {
        foreach (var name in localNames)
        {
            var el = parent.Elements().FirstOrDefault(e =>
                e.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (el != null)
                return el.Value.Trim();
        }

        return null;
    }

    private static string? FindDescendantText(XElement root, params string[] localNames)
    {
        foreach (var name in localNames)
        {
            var el = root.Descendants().FirstOrDefault(e =>
                e.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (el != null)
                return el.Value.Trim();
        }

        return null;
    }
}
