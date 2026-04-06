using HotChocolate.Authorization;
using IIS.Api.Models;
using IIS.Api.Services;

namespace IIS.Api.GraphQL;

public class TaskQueries
{
    [Authorize(Roles = new[] { "Reader", "Full" })]
    public async Task<IReadOnlyList<TaskDto>> Tasks([Service] ITaskOperations ops, CancellationToken ct) =>
        await ops.GetAllAsync(ct).ConfigureAwait(false);

    [Authorize(Roles = new[] { "Reader", "Full" })]
    public async Task<TaskDto?> TaskById([Service] ITaskOperations ops, string id, CancellationToken ct) =>
        await ops.GetByIdAsync(id, ct).ConfigureAwait(false);
}
