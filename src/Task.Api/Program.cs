// Підключаємо FluentValidation для перевірки вхідних HTTP-запитів.
using FluentValidation;
// Підключаємо типи драйвера MongoDB, зокрема IMongoClient та IMongoDatabase.
using MongoDB.Driver;
// Підключаємо Serilog для структурованого логування застосунку.
using Serilog;
// Підключаємо DTO запитів до API.
using Task.Api.Contracts;
// Підключаємо розширення з HTTP-ендпоінтами задач.
using Task.Api.Endpoints;
// Підключаємо валідатори DTO.
using Task.Api.Validators;

using Task.Api.Messaging;

// Створюємо builder для конфігурації вебзастосунку, DI-контейнера та middleware.
var builder = WebApplication.CreateBuilder(args);

// Замінюємо стандартне логування ASP.NET Core на Serilog.
builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
    // Зчитуємо налаштування Serilog з appsettings.json та відповідного environment-файлу.
    .ReadFrom.Configuration(context.Configuration)
    // Дозволяємо Serilog отримувати сервіси з DI-контейнера.
    .ReadFrom.Services(services)
    // Додаємо до кожного запису логу дані поточного контексту запиту.
    .Enrich.FromLogContext()
    // Виводимо логи в консоль, яку збирає Aspire Dashboard.
    .WriteTo.Console());

// Підключаємо спільні налаштування Aspire (Health Checks, OpenTelemetry)
builder.AddServiceDefaults();

// Реєструємо MongoDB-клієнт із connection string ресурсу tasksdb, який передає Aspire.
builder.AddMongoDBClient("tasksdb");

// Додаємо RabbitMQ-клієнт, а Aspire отримує параметри підключення
// з ресурсу rabbitmq, переданого через WithReference в AppHost.
builder.AddRabbitMQClient("rabbitmq");

// Реєструємо IMongoDatabase, щоб endpoint-и могли працювати саме з базою tasksdb.
builder.Services.AddSingleton<IMongoDatabase>(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase("tasksdb"));
// Додаємо метадані endpoint-ів, потрібні для генерації OpenAPI-опису.
builder.Services.AddEndpointsApiExplorer();
// Додаємо Swagger, який показує та дозволяє викликати HTTP API в development-режимі.
builder.Services.AddSwaggerGen();
// Реєструємо валідатор даних для створення задачі.
builder.Services.AddScoped<IValidator<CreateTaskRequest>, CreateTaskRequestValidator>();
// Реєструємо валідатор для повної заміни задачі через PUT.
builder.Services.AddScoped<IValidator<ReplaceTaskRequest>, ReplaceTaskRequestValidator>();
// Реєструємо валідатор для зміни лише статусу через PATCH /status.
builder.Services.AddScoped<IValidator<ChangeTaskStatusRequest>, ChangeTaskStatusRequestValidator>();
// Створюємо RabbitMQ exchange, queue і binding під час запуску API.
builder.Services.AddHostedService<RabbitMqTopologyInitializer>();
// Реєструємо сервіс, через який endpoint-и публікуватимуть події задач.
builder.Services.AddSingleton<ITaskEventPublisher, RabbitMqTaskEventPublisher>();

// Будуємо WebApplication після завершення реєстрації всіх залежностей і налаштувань.
var app = builder.Build();

// Мапимо дефолтні ендпоінти (зокрема /health)
app.MapDefaultEndpoints();

// У development-оточенні відкриваємо Swagger UI для ручного тестування API.
if (app.Environment.IsDevelopment()) {
    // Генеруємо OpenAPI JSON-документ.
    app.UseSwagger();
    // Відображаємо браузерний інтерфейс Swagger.
    app.UseSwaggerUI();
}

// Реєструємо CRUD-маршрути для задач: /tasks, /tasks/{id}.
app.MapTaskEndpoints();

// Запускаємо HTTP-сервер і починаємо приймати запити.
app.Run();
