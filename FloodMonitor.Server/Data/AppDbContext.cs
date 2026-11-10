using FloodMonitor.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FloodMonitor.Server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Occurrence> Occurrences => Set<Occurrence>();
    public DbSet<Feedback> Feedback => Set<Feedback>();
    public DbSet<PageVisit> PageVisits => Set<PageVisit>();
    public DbSet<RainfallReading> RainfallReadings => Set<RainfallReading>();
    public DbSet<RainfallCheckedDay> RainfallCheckedDays => Set<RainfallCheckedDay>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Occurrence>(entity =>
        {
            entity.Property(o => o.WaterLevel).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(o => o.OccurredAt);
            entity.HasIndex(o => o.AuthorHash);
        });

        modelBuilder.Entity<Feedback>().ToTable("Feedback");
        modelBuilder.Entity<PageVisit>().HasIndex(v => v.VisitedAt);

        modelBuilder.Entity<RainfallReading>(entity =>
        {
            entity.Property(r => r.Source).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(r => new { r.Source, r.City, r.State, r.StationCode, r.Day }).IsUnique();
        });

        modelBuilder.Entity<RainfallCheckedDay>(entity =>
        {
            entity.Property(d => d.Source).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(d => new { d.Source, d.City, d.State, d.Day }).IsUnique();
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }
}

public class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value.ToUniversalTime(),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
