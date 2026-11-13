using System.Text;
using System.Threading.RateLimiting;
using LearningMap.Api.Filters;
using LearningMap.Api.Services;
using LearningMap.Application.Interfaces.Services;
using LearningMap.Domain.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace LearningMap.Api.Extensions;

public static class ServiceExtensions
{
    public const string CorsPolicyName = "LocalFrontends";
    public const string LoginRateLimitPolicy = "login";

    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // ---- Controllers + filters ----
        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationFilter>();
            options.Filters.Add<GlobalExceptionFilter>();
        })
        .ConfigureApiBehaviorOptions(options =>
        {
            // Let ValidationFilter build the response in our ApiResponseData shape instead of the default ProblemDetails.
            options.SuppressModelStateInvalidFilter = true;
        });

        // ---- Swagger with a "Authorize" button for the JWT ----
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "LearningMap API", Version = "v1" });
            var scheme = new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Paste the token from POST /auth/login (without the word Bearer).",
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            };
            options.AddSecurityDefinition("Bearer", scheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
        });

        // ---- JWT authentication ----
        var jwt = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
        if (jwt.IssuerSigningKey.Length < 32)
        {
            // Fail fast at startup instead of failing on the first login.
            throw new InvalidOperationException(
                "JwtSettings:IssuerSigningKey is missing or shorter than 32 characters. " +
                "Set it with: dotnet user-secrets set \"JwtSettings:IssuerSigningKey\" \"<a long random string>\" --project src/LearningMap.Api");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false; // keep claim names exactly as written ("sub", "role", ...)
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.ValidIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.ValidAudience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.IssuerSigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = AppClaimTypes.Name,
                    RoleClaimType = AppClaimTypes.Role, // makes [Authorize(Roles = "Admin")] read the "role" claim
                };

                // Return our JSON envelope for 401/403 instead of an empty body (same idea as SafeSpace's OnChallenge).
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(
                            ApiResponseData.Fail(StatusCodes.Status401Unauthorized, "Missing, invalid or expired token."));
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(
                            ApiResponseData.Fail(StatusCodes.Status403Forbidden, "You do not have permission to do this."));
                    },
                };
            });

        services.AddAuthorization();

        // ---- CORS: which browser origins may call this API ----
        var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options => options.AddPolicy(CorsPolicyName, policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

        // ---- Rate limiting (built into .NET 7+): max 5 login attempts per minute per IP ----
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(LoginRateLimitPolicy, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));
        });

        services.AddHealthChecks();

        return services;
    }
}
