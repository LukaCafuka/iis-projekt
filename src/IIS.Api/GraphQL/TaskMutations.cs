using HotChocolate.Authorization;
using IIS.Api.Models;
using IIS.Api.Services;

namespace IIS.Api.GraphQL;

public class TaskMutations
{
    [Authorize(Roles = new[] { "Full" })]
    public async Task<TaskDto> CreateTask([Service] ITaskOperations ops, TaskDto input, CancellationToken ct) =>
        await ops.CreateAsync(input, ct).ConfigureAwait(false);

    [Authorize(Roles = new[] { "Full" })]
    public async Task<TaskDto?> UpdateTask([Service] ITaskOperations ops, string id, TaskDto input, CancellationToken ct) =>
        await ops.UpdateAsync(id, input, ct).ConfigureAwait(false);

    [Authorize(Roles = new[] { "Full" })]
    public async Task<bool> DeleteTask([Service] ITaskOperations ops, string id, CancellationToken ct) =>
        await ops.DeleteAsync(id, ct).ConfigureAwait(false);
}
