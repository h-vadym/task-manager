namespace Task.Contracts.Events;

// Зберігає назви підтримуваних подій, щоб не дублювати рядки в producer і consumer.
public static class TaskEventTypes
{
    public const string Created = "TaskCreated";

    public const string Updated = "TaskUpdated";

    public const string Deleted = "TaskDeleted";
}