using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Task.Analytics.Storage;
using Task.Analytics.Storage.Entities;
using Task.Contracts.Events;

namespace Task.Analytics.Processing;

// Застосовує TaskCreated, TaskUpdated і TaskDeleted до task_type_stats.
public sealed class TaskEventProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<TaskEventProcessor> logger) : ITaskEventProcessor
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async ValueTask ProcessAsync(
        ReadOnlyMemory<byte> messageBody,
        CancellationToken cancellationToken)
    {
        // Спочатку читаємо eventType, щоб правильно десеріалізувати payload.
        using var document = JsonDocument.Parse(messageBody);

        var eventType = document.RootElement
            .GetProperty("eventType")
            .GetString();

        switch (eventType)
        {
            case TaskEventTypes.Created:
                await ProcessCreatedAsync(
                    Deserialize<TaskCreatedPayload>(messageBody),
                    cancellationToken);
                break;

            case TaskEventTypes.Updated:
                await ProcessUpdatedAsync(
                    Deserialize<TaskUpdatedPayload>(messageBody),
                    cancellationToken);
                break;

            case TaskEventTypes.Deleted:
                await ProcessDeletedAsync(
                    Deserialize<TaskDeletedPayload>(messageBody),
                    cancellationToken);
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported task event type: {eventType ?? "<null>"}.");
        }
    }

    private async ValueTask ProcessCreatedAsync(
        TaskEventEnvelope<TaskCreatedPayload> message,
        CancellationToken cancellationToken)
    {
        await ProcessOnceAsync(
            message,
            (database, token) => ApplyDeltaAsync(
                database,
                message.Payload.Type,
                message.Payload.Status,
                delta: 1,
                message.OccurredAt,
                token),
            cancellationToken);
    }

    private async ValueTask ProcessUpdatedAsync(
        TaskEventEnvelope<TaskUpdatedPayload> message,
        CancellationToken cancellationToken)
    {
        await ProcessOnceAsync(
            message,
            async (database, token) =>
            {
                var oldState = message.Payload.Old;
                var newState = message.Payload.New;

                // Якщо type і status не змінились, агрегації не потрібно змінювати.
                if (oldState.Type == newState.Type
                    && oldState.Status == newState.Status)
                {
                    return;
                }

                // Віднімаємо задачу зі старої групи.
                await ApplyDeltaAsync(
                    database,
                    oldState.Type,
                    oldState.Status,
                    delta: -1,
                    message.OccurredAt,
                    token);

                // Додаємо задачу до нової групи.
                await ApplyDeltaAsync(
                    database,
                    newState.Type,
                    newState.Status,
                    delta: 1,
                    message.OccurredAt,
                    token);
            },
            cancellationToken);
    }

    private async ValueTask ProcessDeletedAsync(
        TaskEventEnvelope<TaskDeletedPayload> message,
        CancellationToken cancellationToken)
    {
        await ProcessOnceAsync(
            message,
            (database, token) => ApplyDeltaAsync(
                database,
                message.Payload.Type,
                message.Payload.Status,
                delta: -1,
                message.OccurredAt,
                token),
            cancellationToken);
    }

    private async ValueTask ProcessOnceAsync<TPayload>(
        TaskEventEnvelope<TPayload> message,
        Func<AnalyticsDbContext, CancellationToken, ValueTask> apply,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var database = scope.ServiceProvider
            .GetRequiredService<AnalyticsDbContext>();

        // Idempotency і зміна агрегацій мають відбутися в одній транзакції.
        await using var transaction = await database.Database
            .BeginTransactionAsync(cancellationToken);

        // ON CONFLICT захищає від повторної доставки того самого RabbitMQ-повідомлення.
        var insertedRows = await database.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO processed_messages (event_id, processed_at)
             VALUES ({message.EventId}, {DateTime.UtcNow})
             ON CONFLICT (event_id) DO NOTHING
             """,
            cancellationToken);

        if (insertedRows == 0)
        {
            // Подія вже була застосована раніше: повторно статистику не змінюємо.
            await transaction.RollbackAsync(cancellationToken);

            logger.LogInformation(
                "RabbitMQ event {EventId} was already processed",
                message.EventId);

            return;
        }

        await apply(database, cancellationToken);

        // Зберігаємо змінені агрегати і запис processed_messages.
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Processed RabbitMQ event {EventType} with id {EventId}",
            message.EventType,
            message.EventId);
    }

    private static async ValueTask ApplyDeltaAsync(
        AnalyticsDbContext database,
        string taskType,
        string status,
        int delta,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        var statistic = await database.TaskTypeStats.FindAsync(
            new object?[] { taskType, status },
            cancellationToken);

        if (statistic is null)
        {
            // Нова група може з'явитися лише через створення або переміщення задачі.
            if (delta < 0)
            {
                throw new InvalidOperationException(
                    $"Cannot decrement missing statistic {taskType}/{status}.");
            }

            database.TaskTypeStats.Add(new TaskTypeStat
            {
                TaskType = taskType,
                Status = status,
                TasksCount = delta,
                UpdatedAt = occurredAt
            });

            return;
        }

        var newCount = statistic.TasksCount + delta;

        if (newCount < 0)
        {
            throw new InvalidOperationException(
                $"Statistic {taskType}/{status} cannot become negative.");
        }

        statistic.TasksCount = newCount;
        statistic.UpdatedAt = occurredAt;
    }

    private static TaskEventEnvelope<TPayload> Deserialize<TPayload>(
        ReadOnlyMemory<byte> messageBody)
    {
        return JsonSerializer.Deserialize<TaskEventEnvelope<TPayload>>(
                   messageBody.Span,
                   JsonOptions)
               ?? throw new JsonException("RabbitMQ event cannot be deserialized.");
    }
}