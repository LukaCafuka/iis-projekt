using IIS.Api.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace IIS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class ConfigController(IOptions<TaskApiOptions> options) : ControllerBase
{
    [HttpGet("task-api")]
    public ActionResult<object> GetTaskApiMode() =>
        Ok(new { provider = options.Value.Provider, publicBaseUrl = options.Value.PublicBaseUrl });
}
