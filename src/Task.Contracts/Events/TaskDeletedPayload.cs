namespace Task.Contracts.Events;

// Дані про задачу в момент її soft-delete.
public sealed class TaskDeletedPayload
{
    public required string TaskId { get; init; }

    public required string Type { get; init; }

    public required string Status { get; init; }
}