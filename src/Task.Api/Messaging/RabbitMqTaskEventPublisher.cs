using System.Text.Json;
using RabbitMQ.Client;
using Task.Contracts.Events;
using Task.Contracts.Messaging;

namespace Task.Api.Messaging;

// Відправляє події задач у RabbitMQ.
public sealed class RabbitMqTaskEventPublisher(
    IConnection connection,
    ILogger<RabbitMqTaskEventPublisher> logger) : ITaskEventPublisher
{
    // Web-налаштування дають camelCase у JSON: eventId, occurredAt тощо.
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async ValueTask PublishAsync<TPayload>(
        TaskEventEnvelope<TPayload> message,
        CancellationToken cancellationToken)
    {
        // Перетворюємо envelope на UTF-8 JSON.
        var body = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);

        // IConnection — одне довгоживуче з'єднання з RabbitMQ, спільне для всього API.
        // Channel — легка AMQP-сесія всередині цього з'єднання. Він не призначений для
        // одночасного використання кількома HTTP-запитами, тому для кожної публікації
        // створюємо ізольований channel і закриваємо його після завершення методу.
        // Це не відкриває нове TCP-з'єднання щоразу: використовується той самий IConnection.
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: cancellationToken);

        // Додаємо технічні властивості повідомлення.
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            MessageId = message.EventId.ToString(),
            Type = message.EventType,
            Persistent = true
        };

        // Відправляємо повідомлення в наш exchange через потрібний routing key.
        // mandatory: true означає: RabbitMQ поверне помилку, якщо повідомлення
        // неможливо доставити в жодну чергу.
        await channel.BasicPublishAsync(
            exchange: RabbitMqTopology.ExchangeName,
            routingKey: RabbitMqTopology.TaskEventRoutingKey,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "Published RabbitMQ event {EventType} for task {TaskId}, event id {EventId}",
            message.EventType,
            message.AggregateId,
            message.EventId);
    }
}
