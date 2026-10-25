using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Task.Analytics.Storage;

// Використовується лише командою dotnet ef для генерації migration-файлів.
public sealed class AnalyticsDbContextFactory
    : IDesignTimeDbContextFactory<AnalyticsDbContext>
{
    public AnalyticsDbContext CreateDbContext(string[] args)
    {
        // EF Tools не потребує реального підключення до PostgreSQL під час
        // "migrations add": йому потрібні лише PostgreSQL provider і модель DbContext.
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=analyticsdb;Username=postgres;Password=design-time-only")
            .Options;

        return new AnalyticsDbContext(options);
    }
}