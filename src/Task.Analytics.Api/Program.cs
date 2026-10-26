using Microsoft.EntityFrameworkCore;
using Npgsql;
using Task.Analytics.Api.Storage;
using Task.Analytics.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Підключаємо спільні налаштування Aspire:
// OpenTelemetry, health checks і стандартне логування.
builder.AddServiceDefaults();

// Реєструємо NpgsqlDataSource за connection string analyticsdb,
// який Aspire передає через WithReference(analyticsDatabase).
builder.AddNpgsqlDataSource("analyticsdb");

// Реєструємо read-only EF Core контекст поверх NpgsqlDataSource.
builder.Services.AddDbContext<AnalyticsReadDbContext>(
    (serviceProvider, options) =>
        options.UseNpgsql(
            serviceProvider.GetRequiredService<NpgsqlDataSource>()));

// Додаємо метадані endpoint-ів для Swagger.
builder.Services.AddEndpointsApiExplorer();

// Додаємо Swagger/OpenAPI для ручної перевірки Analytics API.
builder.Services.AddSwaggerGen();
// Дозволяємо клієнтському застосунку читати аналітику з окремого API.
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

// Мапимо стандартні endpoint-и Aspire, зокрема /health та /alive.
app.MapDefaultEndpoints();

// У Development вмикаємо Swagger UI.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Реєструємо endpoint-и для читання аналітичної статистики.
app.MapAnalyticsEndpoints();

app.Run();
