using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Task.Analytics.Processing;
using Task.Contracts.Messaging;
using SystemTask = System.Threading.Tasks.Task;

namespace Task.Analytics;

// Фоновий consumer, який послідовно читає події з черги task.analytics.
public class Worker(
    IConnection connection,
    ITaskEventProcessor processor,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async SystemTask ExecuteAsync(
        CancellationToken stoppingToken)
    {
        // Створюємо канал для цього consumer-а.
        // Один канал використовується протягом життя Worker-а.
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: stoppingToken);

        // RabbitMQ не надсилає наступне повідомлення, поки попереднє
        // не підтверджене через ACK. Це перша частина правила ordering.
        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken);

        // Async consumer дозволяє завершити обробку повідомлення
        // перед його підтвердженням RabbitMQ.
        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                // Обробник оновлює PostgreSQL і фіксує eventId
                // у processed_messages в одній транзакції.
                await processor.ProcessAsync(eventArgs.Body, stoppingToken);

                // ACK надсилаємо лише після успішної обробки та commit у PostgreSQL.
                await channel.BasicAckAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                // Під час штатного завершення не підтверджуємо повідомлення.
                // RabbitMQ поверне його в чергу після закриття з'єднання.
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Failed to process RabbitMQ message {DeliveryTag}",
                    eventArgs.DeliveryTag);

                // Повідомлення не втрачено: повертаємо його в чергу для повторної спроби.
                await channel.BasicNackAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken: CancellationToken.None);
            }
        };

        // autoAck: false означає, що RabbitMQ чекатиме нашого ACK.
        await channel.BasicConsumeAsync(
            queue: RabbitMqTopology.AnalyticsQueueName,
            autoAck: false,
            consumerTag: string.Empty,
            noLocal: false,
            exclusive: false,
            arguments: null,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation(
            "RabbitMQ consumer started for queue {QueueName}",
            RabbitMqTopology.AnalyticsQueueName);

        // Consumer працює через callback ReceivedAsync, тому Worker просто
        // залишається запущеним до зупинки застосунку.
        await SystemTask.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}