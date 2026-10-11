namespace Task.Api.Messaging;

// Зберігає спільні назви RabbitMQ-ресурсів в одному місці.
public static class RabbitMqTopology
{
    // Exchange, у який API публікує всі події задач.
    public const string ExchangeName = "task.events";

    // Черга, з якої майбутній сервіс аналітики читатиме події.
    public const string AnalyticsQueueName = "task.analytics";

    // Routing key, через який exchange спрямовує події до черги.
    public const string TaskEventRoutingKey = "task.event";
}