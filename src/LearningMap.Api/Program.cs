using LearningMap.Api.Extensions;
using LearningMap.Application;
using LearningMap.Infrastructure;
using LearningMap.Infrastructure.Data;
using Serilog;

// Bootstrap logger: catches errors that happen before the real logger is configured.
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    // CreateBuilder already loads, in order (later wins):
    //   appsettings.json -> appsettings.{Environment}.json -> user-secrets (Development only) -> environment variables

    builder.Host.UseSerilog((context, services, logger) => logger
        .ReadFrom.Configuration(context.Configuration)   // "Serilog" section in appsettings
        .ReadFrom.Services(services));

    // ---------- 1. Register services (Dependency Injection container) ----------
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApiServices(builder.Configuration);

    var app = builder.Build();

    // ---------- 2. HTTP request pipeline (middleware). ORDER MATTERS. ----------
    app.UseSerilogRequestLogging(); // one log line per request: method, path, status, duration

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        await app.Services.InitialiseDatabaseAsync(); // migrate + seed admin
    }

    app.UseHttpsRedirection();
    app.UseCors(ServiceExtensions.CorsPolicyName);
    app.UseRateLimiter();
    app.UseAuthentication();  // "who are you?"  (reads + validates the JWT)
    app.UseAuthorization();   // "are you allowed?" ([Authorize] / roles)

    app.MapControllers();
    app.MapHealthChecks("/health");

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException) // `dotnet ef` stops the host on purpose with HostAbortedException
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}
