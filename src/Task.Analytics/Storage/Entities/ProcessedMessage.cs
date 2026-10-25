namespace Task.Analytics.Storage.Entities;

// Запобігає повторній обробці одного RabbitMQ-повідомлення.
public sealed class ProcessedMessage
{
    // EventId із RabbitMQ envelope; первинний ключ гарантує idempotency.
    public Guid EventId { get; set; }

    // Час успішної обробки події.
    public DateTime ProcessedAt { get; set; }
}