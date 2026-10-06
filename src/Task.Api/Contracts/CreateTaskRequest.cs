using Task.Api.Models;

namespace Task.Api.Contracts;

public sealed class CreateTaskRequest
{
    public string? Title { get; init; }

    public string? Description { get; init; }

    public TaskItemType? Type { get; init; }

    public TaskItemStatus? Status { get; init; }

    public TaskItemPriority? Priority { get; init; }

    public string? AssigneeId { get; init; }
}
