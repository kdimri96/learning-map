using LearningMap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LearningMap.Infrastructure.Data;

/// <summary>
/// EF Core's gateway to the database. Each DbSet is a table.
/// The table layout itself lives in Data/Configurations (Fluent API), applied below.
/// </summary>
public class LearningMapDbContext(DbContextOptions<LearningMapDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // A SEQUENCE is a database object that hands out increasing numbers (used for EmployeeCode).
        modelBuilder.HasSequence<int>("EmployeeCodeSequence", "dbo").StartsAt(1).IncrementsBy(1);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LearningMapDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
