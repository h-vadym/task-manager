namespace Task.Analytics.Api.Storage.Entities;

// Один рядок агрегованої статистики для пари type + status.
public sealed class TaskTypeStat
{
    // Тип задачі: design, develop, review або testing.
    public required string TaskType { get; set; }

    // Статус задач: to do, in progress або done.
    public required string Status { get; set; }

    // Кількість задач у відповідній групі.
    public int TasksCount { get; set; }

    // Час останнього оновлення агрегату.
    public DateTime UpdatedAt { get; set; }
}