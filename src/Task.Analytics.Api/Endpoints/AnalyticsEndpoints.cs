using Microsoft.EntityFrameworkCore;
using Task.Analytics.Api.Contracts;
using Task.Analytics.Api.Storage;
using Task.Analytics.Api.Storage.Entities;
using SystemTask = System.Threading.Tasks.Task;

namespace Task.Analytics.Api.Endpoints;

// Містить HTTP endpoints для читання аналітичної проєкції.
public static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(
        this IEndpointRouteBuilder app)
    {
        // Створюємо групу маршрутів зі спільним префіксом /api/analytics.
        var analytics = app.MapGroup("/api/analytics")
            // Групуємо маршрути в окремій секції Swagger.
            .WithTags("Analytics");

        // Повертаємо статистику задач, згруповану за типами.
        analytics.MapGet("/task-types", GetTaskTypeStatistics);

        return app;
    }

    private static async System.Threading.Tasks.Task<IResult> GetTaskTypeStatistics(
        AnalyticsReadDbContext database,
        CancellationToken cancellationToken)
    {
        // Читаємо дані без відстеження змін, бо це виключно read API.
        var statistics = await database.TaskTypeStats
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Перетворюємо рядки type + status на один об'єкт для кожного type.
        var response = statistics
            .GroupBy(statistic => statistic.TaskType)
            .OrderBy(group => group.Key)
            .Select(group => new TaskTypeAnalyticsResponse
            {
                TaskType = group.Key,
                Todo = GetCount(group, "to do"),
                InProgress = GetCount(group, "in progress"),
                Done = GetCount(group, "done")
            })
            .ToList();

        return Results.Ok(response);
    }

    // Повертає лічильник для статусу або 0, якщо такого рядка ще немає.
    private static int GetCount(
        IEnumerable<TaskTypeStat> statistics,
        string status)
    {
        return statistics
            .Where(statistic => statistic.Status == status)
            .Select(statistic => statistic.TasksCount)
            .SingleOrDefault();
    }
}