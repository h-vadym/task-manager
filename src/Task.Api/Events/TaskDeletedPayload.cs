using Task.Api.Models;

namespace Task.Api.Events;

// Дані про задачу на момент її soft-delete.
public sealed class TaskDeletedPayload
{
    public required string TaskId { get; init; }

    public required TaskItemType Type { get; init; }

    public required TaskItemStatus Status { get; init; }
}