namespace Task.Contracts.Events;

// Знімок полів задачі, які аналітика відстежує до або після оновлення.
public sealed class TaskState
{
    public required string Type { get; init; }

    public required string Status { get; init; }
}