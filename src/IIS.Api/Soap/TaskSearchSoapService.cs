using System.Globalization;
using System.Text;
using System.Xml;
using IIS.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace IIS.Api.Soap;

public class TaskSearchSoapService(
    ITaskOperations taskOperations,
    XsdXmlValidator xsd,
    IWebHostEnvironment env,
    IHttpContextAccessor httpContextAccessor) : ITaskSearchSoap
{
    private const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Lower = "abcdefghijklmnopqrstuvwxyz";

    public async Task<TaskSearchSoapResponse> SearchTasksAsync(string searchTerm)
    {
        if (httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated != true
            || !httpContextAccessor.HttpContext.User.IsInRole("Full"))
        {
            throw new UnauthorizedAccessException("Full role required.");
        }

        // Routes through the configured provider (Custom EF / Public MockAPI) per TaskApi:Provider.
        var ct = httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        var tasks = await taskOperations.GetAllAsync(ct).ConfigureAwait(false);
        var xml = TaskXmlBuilder.BuildTasksDocument(tasks);
        var xmlPath = System.IO.Path.Combine(env.ContentRootPath, "Generated", "soap-tasks.xml");
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(xmlPath)!);
        await File.WriteAllTextAsync(xmlPath, xml, Encoding.UTF8, ct).ConfigureAwait(false);

        var validationErrors = xsd.ValidateFile(xmlPath).ToList();
        if (validationErrors.Count > 0)
            return new TaskSearchSoapResponse { ValidationErrors = validationErrors };

        var term = (searchTerm ?? "").Trim().ToLowerInvariant().Replace("'", "");
        var doc = new XmlDocument();
        doc.Load(xmlPath);

        var xpath = string.Create(CultureInfo.InvariantCulture,
            $"//Task[contains(translate(name, '{Upper}', '{Lower}'), '{term}') or contains(translate(description, '{Upper}', '{Lower}'), '{term}')]");
        var nodes = doc.SelectNodes(xpath);
        var response = new TaskSearchSoapResponse();
        if (nodes == null)
            return response;

        foreach (XmlNode node in nodes)
        {
            response.Items.Add(new TaskSoapItem
            {
                Id = GetChild(node, "id"),
                Name = GetChild(node, "name"),
                Description = GetChild(node, "description"),
                Completed = bool.TryParse(GetChild(node, "completed"), out var c) && c,
                CreatedAt = GetChild(node, "createdAt")
            });
        }

        return response;
    }

    private static string? GetChild(XmlNode node, string name)
    {
        foreach (XmlNode child in node.ChildNodes)
        {
            if (child.Name == name)
                return child.InnerText;
        }

        return null;
    }
}
