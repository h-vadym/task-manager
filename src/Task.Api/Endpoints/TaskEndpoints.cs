using FluentValidation;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;
using Task.Api.Contracts;
using Task.Api.Models;
using Task.Contracts.Events;
using Task.Api.Messaging;

namespace Task.Api.Endpoints;

public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        // Створюємо групу маршрутів зі спільним префіксом /api/tasks.
        var tasks = app.MapGroup("/api/tasks")
            // Групуємо маршрути у секції Tasks в Swagger.
            .WithTags("Tasks");

        // Реєструємо маршрут для отримання всіх задач: GET /api/tasks.
        tasks.MapGet("", GetAllTasks);
        // Реєструємо маршрут для отримання однієї задачі: GET /api/tasks/{id}.
        tasks.MapGet("/{id}", GetTaskById);
        // Реєструємо маршрут для створення задачі: POST /api/tasks.
        tasks.MapPost("", CreateTask);
        // Реєструємо маршрут для повної заміни задачі: PUT /api/tasks/{id}.
        tasks.MapPut("/{id}", ReplaceTask);
        // Реєструємо маршрут для зміни лише статусу: PATCH /api/tasks/{id}/status.
        tasks.MapPatch("/{id}/status", ChangeTaskStatus);
        // Реєструємо маршрут для soft-delete задачі: DELETE /api/tasks/{id}.
        tasks.MapDelete("/{id}", DeleteTask);

        // Повертаємо застосунок, щоб за потреби продовжити налаштування маршрутів.
        return app;
    }
    
    private static async Task<IResult> GetTaskById(
        string id,
        IMongoDatabase database,
        CancellationToken cancellationToken)
    {
        // Перевіряємо, що id має формат MongoDB ObjectId.
        if (!ObjectId.TryParse(id, out _))
        {
            return Results.BadRequest(new { message = "Invalid task id." });
        }

        // Шукаємо активну задачу: soft-deleted записи не повертаємо.
        var task = await database
            .GetCollection<TaskItem>("tasks")
            .Find(x => x.Id == id && x.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        // Повертаємо 404, якщо задача відсутня або видалена.
        return task is null ? Results.NotFound() : Results.Ok(task);
    }

    private static async Task<IResult> GetAllTasks(
        IMongoDatabase database,
        CancellationToken cancellationToken)
    {
        // Отримуємо колекцію tasks з підключеної MongoDB-бази.
        var tasks = await database
            .GetCollection<TaskItem>("tasks")
            // Шукаємо лише задачі, які не були позначені як видалені.
            .Find(x => x.DeletedAt == null)
            // Виконуємо запит асинхронно та перетворюємо результат у список.
            .ToListAsync(cancellationToken);

        // Повертаємо список задач зі статусом 200 OK.
        return Results.Ok(tasks);
    }

    private static async Task<IResult> CreateTask(
        CreateTaskRequest request,
        IValidator<CreateTaskRequest> validator,
        IMongoDatabase database,
        // Через publisher надсилаємо подію лише після успішного збереження задачі.
        ITaskEventPublisher eventPublisher,
        CancellationToken cancellationToken)
    {
        // Запускаємо правила FluentValidation для даних, надісланих клієнтом.
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            // Повертаємо 400 Bad Request та перелік помилок валідації.
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        // Перетворюємо DTO запиту на модель, яку зберігатимемо в MongoDB.
        var task = new TaskItem
        {
            Title = request.Title!,
            Description = request.Description!,
            Type = request.Type!.Value,
            Status = request.Status!.Value,
            Priority = request.Priority!.Value,
            AssigneeId = request.AssigneeId,
            // Фіксуємо час створення задачі в UTC.
            CreatedAt = DateTime.UtcNow,
            // Під час створення час оновлення дорівнює часу створення.
            UpdatedAt = DateTime.UtcNow
        };

        // Отримуємо колекцію, у яку буде додано новий документ.
        var tasks = database.GetCollection<TaskItem>("tasks");

        // Асинхронно додаємо задачу до MongoDB.
        await tasks.InsertOneAsync(task, cancellationToken: cancellationToken);

        // MongoDB вже згенерувала Id і зберегла задачу, тому публікуємо TaskCreated.
        await eventPublisher.PublishAsync(
            new TaskEventEnvelope<TaskCreatedPayload>
            {
                EventId = Guid.NewGuid(),
                EventType = "TaskCreated",
                OccurredAt = DateTime.UtcNow,
                AggregateId = task.Id!,
                AggregateType = "Task",
                Version = 1,
                Payload = new TaskCreatedPayload
                {
                    TaskId = task.Id!,
                    Type = ToEventType(task.Type),
                    Status = ToEventStatus(task.Status)
                }
            },
            cancellationToken);

        Log.Information("Task {TaskId} created", task.Id);

        // Повертаємо створену задачу зі статусом 201 Created.
        return Results.Json(task, statusCode: StatusCodes.Status201Created);
    }
    
    private static async Task<IResult> ReplaceTask(
        string id,
        ReplaceTaskRequest request,
        IValidator<ReplaceTaskRequest> validator,
        IMongoDatabase database,
        ITaskEventPublisher eventPublisher,
        CancellationToken cancellationToken)
    {
        // Перевіряємо формат MongoDB id.
        if (!ObjectId.TryParse(id, out _))
        {
            return Results.BadRequest(new { message = "Invalid task id." });
        }

        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var tasks = database.GetCollection<TaskItem>("tasks");

        // Шукаємо лише активну задачу.
        var task = await tasks
            .Find(x => x.Id == id && x.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (task is null)
        {
            return Results.NotFound();
        }

        // Фіксуємо стан, який потрапить у payload.old події TaskUpdated.
        var oldState = new TaskState
        {
            Type = ToEventType(task.Type),
            Status = ToEventStatus(task.Status)
        };

        // PUT замінює повний стан задачі.
        task.Title = request.Title!;
        task.Description = request.Description!;
        task.Type = request.Type!.Value;
        task.Status = request.Status!.Value;
        task.Priority = request.Priority!.Value;
        task.AssigneeId = request.AssigneeId;
        task.UpdatedAt = DateTime.UtcNow;

        await tasks.ReplaceOneAsync(
            x => x.Id == id,
            task,
            cancellationToken: cancellationToken);

        await PublishTaskUpdatedEventAsync(
            task,
            oldState,
            eventPublisher,
            cancellationToken);

        Log.Information("Task {TaskId} replaced", task.Id);

        return Results.Ok(task);
    }

    private static async Task<IResult> ChangeTaskStatus(
        string id,
        ChangeTaskStatusRequest request,
        IValidator<ChangeTaskStatusRequest> validator,
        IMongoDatabase database,
        ITaskEventPublisher eventPublisher,
        CancellationToken cancellationToken)
    {
        // Перевіряємо формат MongoDB id.
        if (!ObjectId.TryParse(id, out _))
        {
            return Results.BadRequest(new { message = "Invalid task id." });
        }

        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var tasks = database.GetCollection<TaskItem>("tasks");

        var task = await tasks
            .Find(x => x.Id == id && x.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (task is null)
        {
            return Results.NotFound();
        }

        // Зберігаємо попередній type/status для події.
        var oldState = new TaskState
        {
            Type = ToEventType(task.Type),
            Status = ToEventStatus(task.Status)
        };

        // PATCH /status змінює тільки статус і час оновлення.
        task.Status = request.Status!.Value;
        task.UpdatedAt = DateTime.UtcNow;

        await tasks.ReplaceOneAsync(
            x => x.Id == id,
            task,
            cancellationToken: cancellationToken);

        await PublishTaskUpdatedEventAsync(
            task,
            oldState,
            eventPublisher,
            cancellationToken);

        Log.Information("Task {TaskId} status changed to {Status}", task.Id, task.Status);

        return Results.Ok(task);
    }

    private static async Task<IResult> DeleteTask(
        string id,
        IMongoDatabase database,
        // Через publisher надсилаємо подію про успішне soft-delete задачі.
        ITaskEventPublisher eventPublisher,
        CancellationToken cancellationToken)
    {
        // Перевіряємо, що параметр маршруту має формат MongoDB ObjectId.
        if (!ObjectId.TryParse(id, out _))
        {
            // Повертаємо 400, якщо формат ідентифікатора некоректний.
            return Results.BadRequest(new { message = "Invalid task id." });
        }

        // Отримуємо колекцію задач із MongoDB.
        var tasks = database.GetCollection<TaskItem>("tasks");
        // Шукаємо лише активну задачу за її ідентифікатором.
        var task = await tasks
            .Find(x => x.Id == id && x.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (task is null)
        {
            // Повертаємо 404, якщо задачі не існує або її вже видалено.
            return Results.NotFound();
        }
        // Зберігаємо потрібні для події поля до позначення задачі як видаленої.
        var deletedPayload = new TaskDeletedPayload
        {
            TaskId = task.Id!,
            Type = ToEventType(task.Type),
            Status = ToEventStatus(task.Status)
        };

        // Позначаємо задачу як видалену, не стираючи документ з MongoDB.
        task.DeletedAt = DateTime.UtcNow;
        // Оновлюємо час останньої зміни задачі.
        task.UpdatedAt = DateTime.UtcNow;

        // Зберігаємо документ із позначкою про видалення.
        await tasks.ReplaceOneAsync(
            x => x.Id == id,
            task,
            cancellationToken: cancellationToken);

        // Після успішного soft-delete у MongoDB публікуємо TaskDeleted.
        await eventPublisher.PublishAsync(
            new TaskEventEnvelope<TaskDeletedPayload>
            {
                EventId = Guid.NewGuid(),
                EventType = "TaskDeleted",
                OccurredAt = DateTime.UtcNow,
                AggregateId = task.Id!,
                AggregateType = "Task",
                Version = 1,
                Payload = deletedPayload
            },
            cancellationToken);

        Log.Information("Task {TaskId} deleted", task.Id);

        // Повертаємо 204 No Content після успішного видалення.
        return Results.NoContent();
    }
    
    // Формує та публікує TaskUpdated після успішної зміни задачі.
    private static ValueTask PublishTaskUpdatedEventAsync(
        TaskItem task,
        TaskState oldState,
        ITaskEventPublisher eventPublisher,
        CancellationToken cancellationToken)
    {
        return eventPublisher.PublishAsync(
            new TaskEventEnvelope<TaskUpdatedPayload>
            {
                EventId = Guid.NewGuid(),
                EventType = TaskEventTypes.Updated,
                OccurredAt = DateTime.UtcNow,
                AggregateId = task.Id!,
                AggregateType = "Task",
                Version = 1,
                Payload = new TaskUpdatedPayload
                {
                    Old = oldState,
                    New = new TaskState
                    {
                        Type = ToEventType(task.Type),
                        Status = ToEventStatus(task.Status)
                    }
                }
            },
            cancellationToken);
    }
    
    // Перетворює внутрішній enum API на стабільне значення transport-контракту.
    private static string ToEventType(TaskItemType type) => type switch
    {
        TaskItemType.Design => "design",
        TaskItemType.Develop => "develop",
        TaskItemType.Review => "review",
        TaskItemType.Testing => "testing",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    // Перетворює внутрішній enum API на стабільне значення transport-контракту.
    private static string ToEventStatus(TaskItemStatus status) => status switch
    {
        TaskItemStatus.ToDo => "to do",
        TaskItemStatus.InProgress => "in progress",
        TaskItemStatus.Done => "done",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}
