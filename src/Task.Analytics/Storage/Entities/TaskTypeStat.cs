namespace Task.Analytics.Storage.Entities;

// Агрегована кількість задач для конкретної пари type + status.
public sealed class TaskTypeStat
{
    // Частина composite primary key, наприклад develop.
    public required string TaskType { get; set; }

    // Частина composite primary key, наприклад done.
    public required string Status { get; set; }

    // Поточна кількість задач із цим type та status.
    public int TasksCount { get; set; }

    // Час останньої зміни агрегату.
    public DateTime UpdatedAt { get; set; }
}