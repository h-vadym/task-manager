namespace Task.Api.Events;

// Універсальна "обгортка" кожної події, яку API надсилатиме в RabbitMQ.
public sealed class TaskEventEnvelope<TPayload>
{
    // Унікальний ідентифікатор конкретно цього повідомлення.
    public required Guid EventId { get; init; }

    // Назва події: TaskCreated, TaskUpdated або TaskDeleted.
    public required string EventType { get; init; }

    // UTC-час, коли відбулася подія.
    public required DateTime OccurredAt { get; init; }

    // Id задачі, з якою сталася подія.
    public required string AggregateId { get; init; }

    // Тип агрегату; для всіх наших подій це буде Task.
    public required string AggregateType { get; init; }

    // Версія формату події для майбутніх змін контракту.
    public required int Version { get; init; }

    // Дані, специфічні для конкретного виду події.
    public required TPayload Payload { get; init; }
}