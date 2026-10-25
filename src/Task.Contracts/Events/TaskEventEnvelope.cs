namespace Task.Contracts.Events;

// Універсальна обгортка для кожної події, що передається через RabbitMQ.
public sealed class TaskEventEnvelope<TPayload>
{
    // Унікальний ідентифікатор конкретного повідомлення.
    public required Guid EventId { get; init; }

    // Назва події: TaskCreated, TaskUpdated або TaskDeleted.
    public required string EventType { get; init; }

    // UTC-час виникнення події.
    public required DateTime OccurredAt { get; init; }

    // Id задачі, до якої належить подія.
    public required string AggregateId { get; init; }

    // Тип агрегату; у цьому модулі завжди Task.
    public required string AggregateType { get; init; }

    // Версія формату події.
    public required int Version { get; init; }

    // Дані, що залежать від конкретного типу події.
    public required TPayload Payload { get; init; }
}