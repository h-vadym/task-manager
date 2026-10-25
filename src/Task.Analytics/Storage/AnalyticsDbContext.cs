using Microsoft.EntityFrameworkCore;
using Task.Analytics.Storage.Entities;

namespace Task.Analytics.Storage;

// EF Core контекст для PostgreSQL analyticsdb.
public sealed class AnalyticsDbContext(
    DbContextOptions<AnalyticsDbContext> options) : DbContext(options)
{
    // Агрегована статистика задач за type і status.
    public DbSet<TaskTypeStat> TaskTypeStats => Set<TaskTypeStat>();

    // Повідомлення, які Worker уже успішно обробив.
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskTypeStat>(entity =>
        {
            entity.ToTable("task_type_stats");

            // Один агрегат для кожної унікальної пари type + status.
            entity.HasKey(x => new { x.TaskType, x.Status });

            entity.Property(x => x.TaskType)
                .HasColumnName("task_type")
                .IsRequired();

            entity.Property(x => x.Status)
                .HasColumnName("status")
                .IsRequired();

            entity.Property(x => x.TasksCount)
                .HasColumnName("tasks_count")
                .HasDefaultValue(0);

            entity.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<ProcessedMessage>(entity =>
        {
            entity.ToTable("processed_messages");

            entity.HasKey(x => x.EventId);

            entity.Property(x => x.EventId)
                .HasColumnName("event_id");

            entity.Property(x => x.ProcessedAt)
                .HasColumnName("processed_at");
        });
    }
}