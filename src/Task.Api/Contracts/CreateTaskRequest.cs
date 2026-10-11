// Підключаємо типи моделі задачі: тип, статус і пріоритет.
using Task.Api.Models;

namespace Task.Api.Contracts;

// Описує тіло HTTP POST /tasks — дані, з яких буде створена нова задача.
public sealed class CreateTaskRequest
{
    // Назва задачі, яку побачить користувач.
    public string? Title { get; init; }

    // Детальний опис того, що потрібно зробити в межах задачі.
    public string? Description { get; init; }

    // Категорія роботи: design, develop, review або testing.
    public TaskItemType? Type { get; init; }

    // Початковий стан задачі: to do, in progress або done.
    public TaskItemStatus? Status { get; init; }

    // Відносна важливість задачі: low, normal або high.
    public TaskItemPriority? Priority { get; init; }

    // Необов'язковий ідентифікатор користувача, відповідального за задачу.
    public string? AssigneeId { get; init; }
}
