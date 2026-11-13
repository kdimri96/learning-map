using LearningMap.Application.Interfaces.Services;
using LearningMap.Domain.Common;
using LearningMap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LearningMap.Infrastructure.Data;

public static class DbInitialiser
{
    /// <summary>
    /// Development-only startup step:
    ///  1. apply any pending EF migrations (creates the database on the first run),
    ///  2. make sure the blob container exists,
    ///  3. create the admin user from the "SeedAdmin" settings if it doesn't exist yet.
    /// </summary>
    public static async Task InitialiseDatabaseAsync(this IServiceProvider services)
    {
        // DbContext is Scoped, and there is no HTTP request at startup, so we create a scope by hand.
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbInitialiser));

        var db = sp.GetRequiredService<LearningMapDbContext>();
        await db.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied");

        try
        {
            await sp.GetRequiredService<IImageStore>().EnsureContainerExistsAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Blob storage not reachable. Is Azurite running? Image uploads will fail until it is.");
        }

        var seed = sp.GetRequiredService<IOptions<SeedAdminSettings>>().Value;
        if (string.IsNullOrWhiteSpace(seed.Email) || string.IsNullOrWhiteSpace(seed.Password))
        {
            logger.LogWarning("SeedAdmin:Email/Password not set; skipping admin seed. See README.");
            return;
        }

        var email = seed.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            return;
        }

        var authService = sp.GetRequiredService<IAuthService>();
        db.Users.Add(new AppUser
        {
            UserGuid = Guid.NewGuid(),
            Email = email,
            FullName = string.IsNullOrWhiteSpace(seed.FullName) ? "Admin" : seed.FullName,
            PasswordHash = authService.HashPassword(seed.Password),
            Role = AppRoles.Admin,
            IsActive = true,
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded admin user {Email}", email);
    }
}
