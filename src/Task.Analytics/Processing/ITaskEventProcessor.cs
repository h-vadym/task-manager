namespace Task.Analytics.Processing;

// Обробляє одне RabbitMQ-повідомлення та застосовує його до PostgreSQL.
public interface ITaskEventProcessor
{
    ValueTask ProcessAsync(
        ReadOnlyMemory<byte> messageBody,
        CancellationToken cancellationToken);
}