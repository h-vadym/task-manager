namespace Task.Analytics.Api.Contracts;

// Статистика задач одного типу, розкладена за доступними статусами.
public sealed class TaskTypeAnalyticsResponse
{
    // Тип задачі: design, develop, review або testing.
    public required string TaskType { get; init; }

    // Кількість задач зі статусом "to do".
    public int Todo { get; init; }

    // Кількість задач зі статусом "in progress".
    public int InProgress { get; init; }

    // Кількість задач зі статусом "done".
    public int Done { get; init; }
}