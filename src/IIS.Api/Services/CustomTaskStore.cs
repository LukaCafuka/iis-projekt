using IIS.Api.Data;
using IIS.Api.Entities;
using IIS.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IIS.Api.Services;

public class CustomTaskStore(ApplicationDbContext db)
{
    public async Task<IReadOnlyList<TaskDto>> GetAllAsync(CancellationToken ct)
    {
        var items = await db.Tasks.AsNoTracking().OrderByDescending(t => t.Id).ToListAsync(ct).ConfigureAwait(false);
        return items.Select(Map).ToList();
    }

    public async Task<TaskDto?> GetByIdAsync(string id, CancellationToken ct)
    {
        if (!int.TryParse(id, out var intId))
            return null;
        var e = await db.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == intId, ct).ConfigureAwait(false);
        return e == null ? null : Map(e);
    }

    public async Task<TaskDto> CreateAsync(TaskDto dto, CancellationToken ct)
    {
        var e = new TaskItem
        {
            ExternalId = dto.Id,
            Name = dto.Name ?? "",
            Description = dto.Description ?? "",
            Completed = dto.Completed,
            CreatedAt = dto.CreatedAt ?? DateTimeOffset.UtcNow
        };
        db.Tasks.Add(e);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Map(e);
    }

    public async Task<TaskDto?> UpdateAsync(string id, TaskDto dto, CancellationToken ct)
    {
        if (!int.TryParse(id, out var intId))
            return null;
        var e = await db.Tasks.FirstOrDefaultAsync(t => t.Id == intId, ct).ConfigureAwait(false);
        if (e == null)
            return null;
        e.Name = dto.Name ?? e.Name;
        e.Description = dto.Description ?? e.Description;
        e.Completed = dto.Completed;
        if (dto.CreatedAt.HasValue)
            e.CreatedAt = dto.CreatedAt.Value;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Map(e);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct)
    {
        if (!int.TryParse(id, out var intId))
            return false;
        var e = await db.Tasks.FirstOrDefaultAsync(t => t.Id == intId, ct).ConfigureAwait(false);
        if (e == null)
            return false;
        db.Tasks.Remove(e);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    private static TaskDto Map(TaskItem t) =>
        new()
        {
            Id = t.Id.ToString(),
            Name = t.Name,
            Description = t.Description,
            Completed = t.Completed,
            CreatedAt = t.CreatedAt
        };
}
