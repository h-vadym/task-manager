using Task.Api.Models;

namespace Task.Api.Contracts;

// Описує повну нову версію задачі для HTTP PUT /api/tasks/{id}.
public sealed class ReplaceTaskRequest
{
    // Нова назва задачі.
    public string? Title { get; init; }

    // Новий опис задачі.
    public string? Description { get; init; }

    // Новий тип задачі.
    public TaskItemType? Type { get; init; }

    // Новий статус задачі.
    public TaskItemStatus? Status { get; init; }

    // Новий пріоритет задачі.
    public TaskItemPriority? Priority { get; init; }

    // Необов'язковий id виконавця.
    public string? AssigneeId { get; init; }
}