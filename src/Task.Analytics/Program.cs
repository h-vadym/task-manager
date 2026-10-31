using Task.Analytics;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Task.Analytics.Storage;
using Task.Analytics.Processing;

var builder = Host.CreateApplicationBuilder(args);

// Підключаємо спільні налаштування Aspire:
// логування, OpenTelemetry та health checks.
builder.AddServiceDefaults();

// Реєструємо RabbitMQ-клієнт і явно обмежуємо обробку
// одним повідомленням за раз, щоб зберігати порядок подій.
builder.AddRabbitMQClient(
    "rabbitmq",
    configureConnectionFactory: factory =>
        factory.ConsumerDispatchConcurrency = 1);

// Реєструємо NpgsqlDataSource для бази analyticsdb.
// Aspire передасть connection string через WithReference(analyticsDatabase).
builder.AddNpgsqlDataSource("analyticsdb");

// Реєструємо EF Core DbContext поверх NpgsqlDataSource,
// який Aspire налаштував через connection string analyticsdb.
builder.Services.AddDbContext<AnalyticsDbContext>((serviceProvider, options) =>
    options.UseNpgsql(serviceProvider.GetRequiredService<NpgsqlDataSource>()));

// Реєструємо processor, який атомарно оновлює PostgreSQL-агрегації.
builder.Services.AddSingleton<ITaskEventProcessor, TaskEventProcessor>();

// Застосовуємо EF Core migrations до запуску RabbitMQ consumer-а.
builder.Services.AddHostedService<DatabaseMigrationService>();

// Реєструємо фоновий сервіс, який стане RabbitMQ consumer-ом.
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();