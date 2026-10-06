using FluentValidation;
using MongoDB.Bson;
using MongoDB.Driver;
using Task.Api.Contracts;
using Task.Api.Models;

namespace Task.Api.Endpoints;

public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        // Створюємо групу маршрутів зі спільним префіксом /tasks.
        var tasks = app.MapGroup("/tasks")
            // Групуємо маршрути у секції Tasks в Swagger.
            .WithTags("Tasks");

        // Реєструємо маршрут для отримання всіх задач: GET /tasks.
        tasks.MapGet("", GetAllTasks);
        // Реєструємо маршрут для створення задачі: POST /tasks.
        tasks.MapPost("", CreateTask);
        // Реєструємо маршрут для часткового оновлення задачі: PATCH /tasks/{id}.
        tasks.MapPatch("/{id}", UpdateTask);
        // Реєструємо маршрут для soft delete задачі: DELETE /tasks/{id}.
        tasks.MapDelete("/{id}", DeleteTask);

        // Повертаємо застосунок, щоб за потреби продовжити налаштування маршрутів.
        return app;
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

        // Повертаємо створену задачу зі статусом 201 Created.
        return Results.Json(task, statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> UpdateTask(
        string id,
        UpdateTaskRequest request,
        IValidator<UpdateTaskRequest> validator,
        IMongoDatabase database,
        CancellationToken cancellationToken)
    {
        // Перевіряємо, що параметр маршруту має формат MongoDB ObjectId.
        if (!ObjectId.TryParse(id, out _))
        {
            // Повертаємо 400, якщо формат ідентифікатора некоректний.
            return Results.BadRequest(new { message = "Invalid task id." });
        }

        // Запускаємо правила FluentValidation для даних оновлення.
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            // Повертаємо 400 Bad Request та перелік помилок валідації.
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        // Отримуємо колекцію задач із MongoDB.
        var tasks = database.GetCollection<TaskItem>("tasks");
        // Шукаємо задачу за її ідентифікатором.
        var task = await tasks
            // Не дозволяємо оновлювати задачу, яку вже видалено.
            .Find(x => x.Id == id && x.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (task is null)
        {
            // Повертаємо 404, якщо задачі з таким id не існує.
            return Results.NotFound();
        }

        // Оновлюємо title лише тоді, коли клієнт передав це поле.
        if (request.Title is not null)
        {
            task.Title = request.Title;
        }

        // Оновлюємо description лише тоді, коли клієнт передав це поле.
        if (request.Description is not null)
        {
            task.Description = request.Description;
        }

        // Оновлюємо type лише тоді, коли клієнт передав це поле.
        if (request.Type is not null)
        {
            task.Type = request.Type.Value;
        }

        // Оновлюємо status лише тоді, коли клієнт передав це поле.
        if (request.Status is not null)
        {
            task.Status = request.Status.Value;
        }

        // Оновлюємо priority лише тоді, коли клієнт передав це поле.
        if (request.Priority is not null)
        {
            task.Priority = request.Priority.Value;
        }

        // Оновлюємо assigneeId лише тоді, коли клієнт передав це поле.
        if (request.AssigneeId is not null)
        {
            task.AssigneeId = request.AssigneeId;
        }
        // Фіксуємо час останньої зміни задачі в UTC.
        task.UpdatedAt = DateTime.UtcNow;

        // Замінюємо знайдений документ його оновленою версією.
        await tasks.ReplaceOneAsync(
            x => x.Id == id,
            task,
            cancellationToken: cancellationToken);

        // Повертаємо оновлену задачу зі статусом 200 OK.
        return Results.Ok(task);
    }

    private static async Task<IResult> DeleteTask(
        string id,
        IMongoDatabase database,
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

        // Позначаємо задачу як видалену, не стираючи документ з MongoDB.
        task.DeletedAt = DateTime.UtcNow;
        // Оновлюємо час останньої зміни задачі.
        task.UpdatedAt = DateTime.UtcNow;

        // Зберігаємо документ із позначкою про видалення.
        await tasks.ReplaceOneAsync(
            x => x.Id == id,
            task,
            cancellationToken: cancellationToken);

        // Повертаємо 204 No Content після успішного видалення.
        return Results.NoContent();
    }
}
