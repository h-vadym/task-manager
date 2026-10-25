namespace Task.Contracts.Events;

// Дані, що надсилаються після створення задачі.
public sealed class TaskCreatedPayload
{
    public required string TaskId { get; init; }

    // Значення transport-контракту, наприклад: develop.
    public required string Type { get; init; }

    // Значення transport-контракту, наприклад: to do.
    public required string Status { get; init; }
}