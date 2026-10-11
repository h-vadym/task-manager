using Task.Api.Models;

namespace Task.Api.Events;

// Дані, що надсилаються після створення задачі.
public sealed class TaskCreatedPayload
{
    public required string TaskId { get; init; }

    public required TaskItemType Type { get; init; }

    public required TaskItemStatus Status { get; init; }
}