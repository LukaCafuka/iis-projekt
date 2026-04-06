using IIS.Api.Models;
using IIS.Api.Options;
using Microsoft.Extensions.Options;

namespace IIS.Api.Services;

public interface ITaskOperations
{
    Task<IReadOnlyList<TaskDto>> GetAllAsync(CancellationToken ct = default);
    Task<TaskDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<TaskDto> CreateAsync(TaskDto dto, CancellationToken ct = default);
    Task<TaskDto?> UpdateAsync(string id, TaskDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
}

public class TaskOperationsFacade : ITaskOperations
{
    private readonly TaskApiOptions _options;
    private readonly PublicTaskApiClient _public;
    private readonly CustomTaskStore _custom;

    public TaskOperationsFacade(IOptions<TaskApiOptions> options, PublicTaskApiClient publicClient, CustomTaskStore custom)
    {
        _options = options.Value;
        _public = publicClient;
        _custom = custom;
    }

    private bool UsePublic => string.Equals(_options.Provider, "Public", StringComparison.OrdinalIgnoreCase);

    public Task<IReadOnlyList<TaskDto>> GetAllAsync(CancellationToken ct = default) =>
        UsePublic ? _public.GetAllAsync(ct) : _custom.GetAllAsync(ct);

    public Task<TaskDto?> GetByIdAsync(string id, CancellationToken ct = default) =>
        UsePublic ? _public.GetByIdAsync(id, ct) : _custom.GetByIdAsync(id, ct);

    public Task<TaskDto> CreateAsync(TaskDto dto, CancellationToken ct = default) =>
        UsePublic ? _public.CreateAsync(dto, ct) : _custom.CreateAsync(dto, ct);

    public Task<TaskDto?> UpdateAsync(string id, TaskDto dto, CancellationToken ct = default) =>
        UsePublic ? _public.UpdateAsync(id, dto, ct) : _custom.UpdateAsync(id, dto, ct);

    public Task<bool> DeleteAsync(string id, CancellationToken ct = default) =>
        UsePublic ? _public.DeleteAsync(id, ct) : _custom.DeleteAsync(id, ct);
}
