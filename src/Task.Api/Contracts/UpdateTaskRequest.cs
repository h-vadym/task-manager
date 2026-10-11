// Підключаємо типи моделі задачі: тип, статус і пріоритет.
using Task.Api.Models;

namespace Task.Api.Contracts;

// Описує тіло HTTP PATCH /tasks/{id} для часткового оновлення наявної задачі.
public sealed class UpdateTaskRequest
{
    // Нова назва задачі; null означає, що назву не потрібно змінювати.
    public string? Title { get; init; }

    // Новий опис задачі; null означає, що опис не потрібно змінювати.
    public string? Description { get; init; }

    // Новий тип задачі; null означає, що тип не потрібно змінювати.
    public TaskItemType? Type { get; init; }

    // Новий статус задачі; null означає, що статус не потрібно змінювати.
    public TaskItemStatus? Status { get; init; }

    // Новий пріоритет задачі; null означає, що пріоритет не потрібно змінювати.
    public TaskItemPriority? Priority { get; init; }

    // Новий ідентифікатор виконавця; null означає, що виконавця не потрібно змінювати.
    public string? AssigneeId { get; init; }
}
