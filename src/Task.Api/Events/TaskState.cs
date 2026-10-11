using Task.Api.Models;

namespace Task.Api.Events;

// Стан полів задачі, зміна яких цікавить аналітику.
public sealed class TaskState
{
    public required TaskItemType Type { get; init; }

    public required TaskItemStatus Status { get; init; }
}