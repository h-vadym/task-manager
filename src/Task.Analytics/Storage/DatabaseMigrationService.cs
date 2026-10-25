using Microsoft.EntityFrameworkCore;
using SystemTask = System.Threading.Tasks.Task;

namespace Task.Analytics.Storage;

// Застосовує всі ще не виконані EF Core migrations під час старту Worker-а.
public sealed class DatabaseMigrationService(
    IServiceScopeFactory scopeFactory,
    ILogger<DatabaseMigrationService> logger) : IHostedService
{
    public async SystemTask StartAsync(CancellationToken cancellationToken)
    {
        // DbContext має scoped lifetime, тому створюємо окремий DI scope.
        await using var scope = scopeFactory.CreateAsyncScope();

        var database = scope.ServiceProvider
            .GetRequiredService<AnalyticsDbContext>();

        // Створює таблицю __EFMigrationsHistory, якщо її ще немає,
        // і застосовує InitialAnalyticsSchema до PostgreSQL analyticsdb.
        await database.Database.MigrateAsync(cancellationToken);

        logger.LogInformation("EF Core migrations applied to analyticsdb");
    }

    // Під час зупинки Worker-а додаткових дій не потрібно.
    public SystemTask StopAsync(CancellationToken cancellationToken) =>
        SystemTask.CompletedTask;
}