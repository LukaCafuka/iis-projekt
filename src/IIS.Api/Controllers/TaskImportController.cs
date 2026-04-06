using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using IIS.Api.Models;
using IIS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IIS.Api.Controllers;

[ApiController]
[Route("api/tasks/import")]
[Authorize(Roles = "Full")]
public class TaskImportController(
    XsdXmlValidator xsd,
    JsonSchemaImportValidator jsonSchema,
    CustomTaskStore store) : ControllerBase
{
    [HttpPost("xml")]
    [Consumes("application/xml", "text/xml")]
    public async Task<IActionResult> ImportXml(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var xml = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        var errors = xsd.Validate(xml).ToList();
        if (errors.Count > 0)
            return BadRequest(new ValidationErrorResponse { Errors = errors });

        List<TaskDto> dtos;
        try
        {
            dtos = ParseTasksXml(xml);
        }
        catch (Exception ex)
        {
            return BadRequest(new ValidationErrorResponse { Errors = new List<string> { ex.Message } });
        }

        var created = new List<TaskDto>();
        foreach (var dto in dtos)
            created.Add(await store.CreateAsync(dto, ct).ConfigureAwait(false));

        return Ok(created);
    }

    [HttpPost("xml/upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportXmlUpload([FromForm] IFormFile? file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ValidationErrorResponse { Errors = new List<string> { "Empty file." } });
        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct).ConfigureAwait(false);
        var xml = System.Text.Encoding.UTF8.GetString(ms.ToArray());
        var errors = xsd.Validate(xml).ToList();
        if (errors.Count > 0)
            return BadRequest(new ValidationErrorResponse { Errors = errors });

        List<TaskDto> dtos;
        try
        {
            dtos = ParseTasksXml(xml);
        }
        catch (Exception ex)
        {
            return BadRequest(new ValidationErrorResponse { Errors = new List<string> { ex.Message } });
        }

        var created = new List<TaskDto>();
        foreach (var dto in dtos)
            created.Add(await store.CreateAsync(dto, ct).ConfigureAwait(false));

        return Ok(created);
    }

    [HttpPost("json")]
    [Consumes("application/json")]
    public async Task<IActionResult> ImportJson(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        var errors = jsonSchema.Validate(json).ToList();
        if (errors.Count > 0)
            return BadRequest(new ValidationErrorResponse { Errors = errors });

        List<TaskDto> dtos;
        try
        {
            dtos = ParseTasksJson(json);
        }
        catch (Exception ex)
        {
            return BadRequest(new ValidationErrorResponse { Errors = new List<string> { ex.Message } });
        }

        var created = new List<TaskDto>();
        foreach (var dto in dtos)
            created.Add(await store.CreateAsync(dto, ct).ConfigureAwait(false));

        return Ok(created);
    }

    [HttpPost("json/upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportJsonUpload([FromForm] IFormFile? file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ValidationErrorResponse { Errors = new List<string> { "Empty file." } });
        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct).ConfigureAwait(false);
        var json = System.Text.Encoding.UTF8.GetString(ms.ToArray());
        var errors = jsonSchema.Validate(json).ToList();
        if (errors.Count > 0)
            return BadRequest(new ValidationErrorResponse { Errors = errors });

        List<TaskDto> dtos;
        try
        {
            dtos = ParseTasksJson(json);
        }
        catch (Exception ex)
        {
            return BadRequest(new ValidationErrorResponse { Errors = new List<string> { ex.Message } });
        }

        var created = new List<TaskDto>();
        foreach (var dto in dtos)
            created.Add(await store.CreateAsync(dto, ct).ConfigureAwait(false));

        return Ok(created);
    }

    private static List<TaskDto> ParseTasksXml(string xml)
    {
        var doc = XDocument.Parse(xml);
        var root = doc.Root ?? throw new InvalidOperationException("Missing root element.");
        if (root.Name.LocalName != "Tasks")
            throw new InvalidOperationException("Root element must be Tasks.");

        var list = new List<TaskDto>();
        foreach (var el in root.Elements().Where(e => e.Name.LocalName == "Task"))
        {
            list.Add(new TaskDto
            {
                Id = Element(el, "id"),
                Name = Element(el, "name") ?? "",
                Description = Element(el, "description") ?? "",
                Completed = bool.TryParse(Element(el, "completed"), out var c) && c,
                CreatedAt = DateTimeOffset.TryParse(Element(el, "createdAt"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt)
                    ? dt
                    : null
            });
        }

        return list;
    }

    private static string? Element(XElement parent, string name) =>
        parent.Elements().FirstOrDefault(e => e.Name.LocalName == name)?.Value;

    private static List<TaskDto> ParseTasksJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var list = new List<TaskDto>();
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in root.EnumerateArray())
                list.Add(ParseTaskElement(el));
        }
        else
        {
            list.Add(ParseTaskElement(root));
        }

        return list;
    }

    private static TaskDto ParseTaskElement(JsonElement el)
    {
        return new TaskDto
        {
            Id = el.TryGetProperty("id", out var id) ? id.GetString() : null,
            Name = el.TryGetProperty("name", out var n) ? n.GetString() : "",
            Description = el.TryGetProperty("description", out var d) ? d.GetString() : "",
            Completed = el.TryGetProperty("completed", out var c) && c.GetBoolean(),
            CreatedAt = el.TryGetProperty("createdAt", out var ca) && DateTimeOffset.TryParse(ca.GetString(), out var dto)
                ? dto
                : null
        };
    }
}
