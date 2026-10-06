using FluentValidation;
using Serilog;
using Task.Api.Contracts;
using Task.Api.Endpoints;
using Task.Api.Validators;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Підключаємо спільні налаштування Aspire (Health Checks, OpenTelemetry)
builder.AddServiceDefaults();

// Додаємо підтримку MongoDB
builder.AddMongoDBClient("tasksdb");

// Додаємо підтримку Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IValidator<CreateTaskRequest>, CreateTaskRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateTaskRequest>, UpdateTaskRequestValidator>();

var app = builder.Build();

// Мапимо дефолтні ендпоінти (зокрема /health)
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapTaskEndpoints();

app.Run();
