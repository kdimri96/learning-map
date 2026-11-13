using LearningMap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearningMap.Infrastructure.Data.Configurations;

// Fluent API: column types, lengths, keys and indexes. This is what the migration turns into SQL.

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.ToTable("Users");
        b.HasKey(u => u.UserId);
        b.HasIndex(u => u.UserGuid).IsUnique();
        b.HasIndex(u => u.Email).IsUnique();

        b.Property(u => u.FullName).HasMaxLength(100).IsRequired();
        b.Property(u => u.Email).HasMaxLength(256).IsRequired();
        b.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
        b.Property(u => u.Role).HasMaxLength(50).IsRequired();
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> b)
    {
        b.ToTable("Employees");
        b.HasKey(e => e.EmployeeId);
        b.HasIndex(e => e.EmployeeGuid).IsUnique();

        // EmployeeCode = 'EMP-' + 5-digit number from a database SEQUENCE.
        // ValueGeneratedOnAdd + default SQL: EF leaves the column out of the INSERT and reads
        // the generated value back (via OUTPUT INSERTED...), so the C# object gets the code too.
        b.Property(e => e.EmployeeCode)
            .HasMaxLength(20)
            .HasDefaultValueSql("'EMP-' + RIGHT('00000' + CAST(NEXT VALUE FOR dbo.EmployeeCodeSequence AS varchar(10)), 5)")
            .ValueGeneratedOnAdd();
        b.HasIndex(e => e.EmployeeCode).IsUnique();

        // Filtered unique index: email must be unique among NON-deleted employees only,
        // so a soft-deleted employee's email can be reused.
        b.HasIndex(e => e.Email).IsUnique().HasFilter("[IsDeleted] = 0");
        b.HasIndex(e => e.Department);

        b.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
        b.Property(e => e.LastName).HasMaxLength(100).IsRequired();
        b.Property(e => e.Email).HasMaxLength(256).IsRequired();
        b.Property(e => e.PhoneNumber).HasMaxLength(20);
        b.Property(e => e.Department).HasMaxLength(100).IsRequired();
        b.Property(e => e.Designation).HasMaxLength(100).IsRequired();
        b.Property(e => e.DateOfJoining).HasColumnType("date");
        b.Property(e => e.ProfileImageBlobName).HasMaxLength(300);

        // Global query filter: EVERY LINQ query on Employees gets "WHERE IsDeleted = 0" automatically.
        // (Use .IgnoreQueryFilters() when you really need deleted rows.)
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}
