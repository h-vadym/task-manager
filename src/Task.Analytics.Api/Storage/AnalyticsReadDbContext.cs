using Microsoft.EntityFrameworkCore;
using Task.Analytics.Api.Storage.Entities;

namespace Task.Analytics.Api.Storage;

// Read-only EF Core контекст для читання аналітичної проєкції з PostgreSQL.
public sealed class AnalyticsReadDbContext(
    DbContextOptions<AnalyticsReadDbContext> options) : DbContext(options)
{
    // Надає доступ до таблиці task_type_stats.
    public DbSet<TaskTypeStat> TaskTypeStats => Set<TaskTypeStat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskTypeStat>(entity =>
        {
            // Використовуємо вже створену Worker-ом таблицю.
            entity.ToTable("task_type_stats");

            // Первинний ключ — унікальна пара type + status.
            entity.HasKey(x => new { x.TaskType, x.Status });

            entity.Property(x => x.TaskType)
                .HasColumnName("task_type")
                .IsRequired();

            entity.Property(x => x.Status)
                .HasColumnName("status")
                .IsRequired();

            entity.Property(x => x.TasksCount)
                .HasColumnName("tasks_count");

            entity.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at");
        });
    }
}