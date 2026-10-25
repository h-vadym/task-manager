using Task.Contracts.Events;

namespace Task.Api.Messaging;

// Описує сервіс, який надсилає події задач у RabbitMQ.
public interface ITaskEventPublisher
{
    // Публікує готовий envelope з довільним payload.
    ValueTask PublishAsync<TPayload>(
        TaskEventEnvelope<TPayload> message,
        CancellationToken cancellationToken);
}