using RabbitMQ.Client;
using Task.Contracts.Messaging;

namespace Task.Api.Messaging;

// Створює exchange, queue і binding під час старту Task.Api.
public sealed class RabbitMqTopologyInitializer(
    IConnection connection,
    ILogger<RabbitMqTopologyInitializer> logger) : IHostedService
{
    public async System.Threading.Tasks.Task StartAsync(CancellationToken cancellationToken)
    {
        // Створюємо окремий RabbitMQ channel для налаштування topology.
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: cancellationToken);

        // Оголошуємо durable topic exchange.
        // Durable означає, що exchange переживе перезапуск RabbitMQ.
        await channel.ExchangeDeclareAsync(
            exchange: RabbitMqTopology.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            passive: false,
            noWait: false,
            cancellationToken: cancellationToken);

        // Оголошуємо durable чергу для сервісу аналітики.
        await channel.QueueDeclareAsync(
            queue: RabbitMqTopology.AnalyticsQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            passive: false,
            noWait: false,
            cancellationToken: cancellationToken);

        // Зв'язуємо чергу з exchange: події з routing key task.event
        // потраплятимуть саме до task.analytics.
        await channel.QueueBindAsync(
            queue: RabbitMqTopology.AnalyticsQueueName,
            exchange: RabbitMqTopology.ExchangeName,
            routingKey: RabbitMqTopology.TaskEventRoutingKey,
            arguments: null,
            noWait: false,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "RabbitMQ topology initialized: exchange {Exchange}, queue {Queue}, routing key {RoutingKey}",
            RabbitMqTopology.ExchangeName,
            RabbitMqTopology.AnalyticsQueueName,
            RabbitMqTopology.TaskEventRoutingKey);
    }

    // Під час зупинки API окремих дій не потрібно:
    // channel автоматично закриється після завершення StartAsync.
    public System.Threading.Tasks.Task StopAsync(CancellationToken cancellationToken) =>
        System.Threading.Tasks.Task.CompletedTask;
}
