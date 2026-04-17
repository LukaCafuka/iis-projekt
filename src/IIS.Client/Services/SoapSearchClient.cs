using System.Xml.Linq;

namespace IIS.Client.Services;

public class SoapSearchClient(ApiClientWithAuthFactory apiClientFactory)
{
    public async Task<(List<SoapTaskRow> Items, List<string> Errors, string? RawFault)> SearchAsync(string term, CancellationToken ct = default)
    {
        var safe = System.Security.SecurityElement.Escape(term ?? "");
        var body = $"""
<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Body>
    <SearchTasks xmlns="http://iis.local/soap/tasks">
      <searchTerm>{safe}</searchTerm>
    </SearchTasks>
  </soap:Body>
</soap:Envelope>
""";
  using var request = new HttpRequestMessage(HttpMethod.Post, "soap/TaskSearch.svc")
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "text/xml")
        };
        request.Headers.TryAddWithoutValidation("SOAPAction", "\"http://iis.local/soap/tasks/ITaskSearchSoap/SearchTasks\"");

  var client = apiClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(60);
        var response = await client.SendAsync(request, ct).ConfigureAwait(false);
        var xml = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        return ([], [], ResponseFormatter.FormatHttpResponse(response, xml));

        var doc = XDocument.Parse(xml);
        XNamespace ns = "http://iis.local/soap/tasks";
        var validationErrors = doc.Descendants(ns + "ValidationErrors").SelectMany(e => e.Elements().Select(x => x.Value)).ToList();
        var items = doc.Descendants(ns + "TaskSoapItem").Select(ParseItem).ToList();
        return (items, validationErrors, null);
    }

    private static SoapTaskRow ParseItem(XElement el)
    {
        XNamespace ns = "http://iis.local/soap/tasks";
        string? T(string name) => el.Element(ns + name)?.Value ?? el.Elements().FirstOrDefault(e => e.Name.LocalName == name)?.Value;
        return new SoapTaskRow(
            T("Id"),
            T("Name"),
            T("Description"),
            bool.TryParse(T("Completed"), out var c) && c,
            T("CreatedAt"));
    }
}

public record SoapTaskRow(string? Id, string? Name, string? Description, bool Completed, string? CreatedAt);
