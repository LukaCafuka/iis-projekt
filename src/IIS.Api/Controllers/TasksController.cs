using IIS.Api.Models;
using IIS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IIS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController(ITaskOperations tasks) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Reader,Full")]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> GetAll(CancellationToken ct)
    {
        return Ok(await tasks.GetAllAsync(ct).ConfigureAwait(false));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Reader,Full")]
    public async Task<ActionResult<TaskDto>> GetById(string id, CancellationToken ct)
    {
        var t = await tasks.GetByIdAsync(id, ct).ConfigureAwait(false);
        return t == null ? NotFound() : Ok(t);
    }

    [HttpPost]
    [Authorize(Roles = "Full")]
    public async Task<ActionResult<TaskDto>> Create([FromBody] TaskDto dto, CancellationToken ct)
    {
        var created = await tasks.CreateAsync(dto, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(created.Id))
            return Ok(created);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Full")]
    public async Task<ActionResult<TaskDto>> Update(string id, [FromBody] TaskDto dto, CancellationToken ct)
    {
        var updated = await tasks.UpdateAsync(id, dto, ct).ConfigureAwait(false);
        return updated == null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Full")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var ok = await tasks.DeleteAsync(id, ct).ConfigureAwait(false);
        return ok ? NoContent() : NotFound();
    }
}
