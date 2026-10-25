using Task.Api.Models;

namespace Task.Api.Contracts;

// Описує тіло HTTP PATCH /api/tasks/{id}/status.
public sealed class ChangeTaskStatusRequest
{
    // Новий статус задачі.
    public TaskItemStatus? Status { get; init; }
}