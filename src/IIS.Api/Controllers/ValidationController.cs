using IIS.Api.Models;
using IIS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IIS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Full")]
public class ValidationController(
    XsdXmlValidator xsd,
    JsonSchemaImportValidator jsonSchema) : ControllerBase
{
    [HttpPost("xml")]
    [Consumes("application/xml", "text/xml")]
    public async Task<IActionResult> ValidateXml(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var xml = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        var errors = xsd.Validate(xml).ToList();
        if (errors.Count == 0)
            return Ok(new { valid = true, errors = Array.Empty<string>() });
        return BadRequest(new ValidationErrorResponse { Errors = errors });
    }

    [HttpPost("json")]
    [Consumes("application/json", "text/json")]
    public async Task<IActionResult> ValidateJson(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        var errors = jsonSchema.Validate(json).ToList();
        if (errors.Count == 0)
            return Ok(new { valid = true, errors = Array.Empty<string>() });
        return BadRequest(new ValidationErrorResponse { Errors = errors });
    }
}
