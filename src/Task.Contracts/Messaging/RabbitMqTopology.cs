namespace Task.Contracts.Messaging;

// Спільні назви RabbitMQ-ресурсів для producer-а та consumer-а.
public static class RabbitMqTopology
{
    // Exchange, у який Task.Api публікує події.
    public const string ExchangeName = "task.events";

    // Черга, яку читає Task.Analytics.
    public const string AnalyticsQueueName = "task.analytics";

    // Routing key для всіх подій задач.
    public const string TaskEventRoutingKey = "task.event";
}