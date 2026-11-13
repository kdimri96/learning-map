using Azure.Storage.Blobs;
using LearningMap.Application.Interfaces.Repositories;
using LearningMap.Application.Interfaces.Services;
using LearningMap.Domain.Common;
using LearningMap.Infrastructure.Data;
using LearningMap.Infrastructure.Repositories;
using LearningMap.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LearningMap.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ---- Options pattern: bind appsettings sections to typed classes ----
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<BlobStorageSettings>(configuration.GetSection(BlobStorageSettings.SectionName));
        services.Configure<SeedAdminSettings>(configuration.GetSection(SeedAdminSettings.SectionName));

        // ---- Database (EF Core + SQL Server) ----
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        services.AddScoped<AuditableEntityInterceptor>();
        services.AddDbContext<LearningMapDbContext>((sp, options) => options
            .UseSqlServer(connectionString)
            .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>()));

        // ---- Lifetimes (an important interview topic) ----
        // Scoped    = one instance per HTTP request (DbContext and anything that uses it).
        // Singleton = one instance for the whole app (stateless or thread-safe things).
        // Transient = a new instance every time it is asked for.
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddSingleton<IAuthService, AuthService>();

        // ---- Azure Blob Storage (Azurite locally) ----
        var blobConnectionString = configuration.GetConnectionString("BlobStorage")
            ?? throw new InvalidOperationException("Connection string 'BlobStorage' is missing.");
        services.AddSingleton(_ => new BlobServiceClient(blobConnectionString)); // thread-safe, reuse it
        services.AddSingleton<IImageStore, AzureBlobImageStore>();

        return services;
    }
}
