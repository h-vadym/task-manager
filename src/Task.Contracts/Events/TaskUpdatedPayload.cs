namespace Task.Contracts.Events;

// Дані про стан задачі до та після оновлення.
public sealed class TaskUpdatedPayload
{
    public required TaskState Old { get; init; }

    public required TaskState New { get; init; }
}