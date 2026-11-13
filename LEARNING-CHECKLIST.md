# Learning checklist: .NET backend (SafeSpace style)

`[x]` = built in learning-map, so you can show it. `[ ]` = next to learn.
Each item says **where to look** in learning-map, and in SafeSpace where it matches.

---

## 1. Setup & tooling
- [x] Installed .NET 8 SDK, SQL Server Express, Azurite, `dotnet-ef`, VS Code C#/SQL/REST extensions
- [x] `dotnet` CLI: `new`, `sln add`, `build`, `run`, `test`, `user-secrets`, `ef`
- [x] Solution (`.sln`) with multiple projects (`.csproj`), and project references between them
- [x] NuGet packages and pinning versions to match SafeSpace (`global.json`, each `.csproj`)
- [x] `Directory.Build.props` to share `net8.0` / `Nullable` / `ImplicitUsings` across projects
- [x] `launchSettings.json` profiles (https/http ports, `ASPNETCORE_ENVIRONMENT=Development`)

## 2. C# features used
- [x] Classes, records, interfaces, abstract classes, generics (`GenericRepository<T>`)
- [x] `async`/`await`, `Task`, `CancellationToken` passed end to end
- [x] Why `async void` is a bug (see the comment in `Repositories.cs`; SafeSpace's `GenericRepository.Create` has it)
- [x] Nullable reference types (`string?`), `required`, `init`
- [x] Primary constructors (C# 12): `public class LoginCommandHandler(IUnitOfWork unitOfWork, ...)`
- [x] LINQ + expression trees (`Expression<Func<T,bool>>` filters in repositories)
- [x] Tuples `(Items, TotalCount)`, pattern matching (`is null`, `switch` expressions), collection expressions `[...]`
- [x] `Span<byte>` / `stackalloc` for reading file headers (`ImageSignature.cs`)

## 3. ASP.NET Core Web API basics
- [x] `Program.cs`: the builder, service registration, `app` pipeline
- [x] Middleware pipeline and **why order matters** (Authentication before Authorization, CORS before both)
- [x] Controllers, `[ApiController]`, `[Route]`, `[HttpGet/Post/Put/Delete]`, route constraints `{id:guid}`
- [x] Model binding: `[FromBody]` (JSON), `[FromQuery]`, `[FromForm]` (multipart), `[FromRoute]`, `[BindNever]`
- [x] Returning correct HTTP status codes (200/201/400/401/403/404/409/429/500) via `ToActionResult`
- [x] One response envelope `ApiResponseData`, the same as SafeSpace
- [x] Action filter (`ValidationFilter`) and exception filter (`GlobalExceptionFilter`), so controllers need no try/catch
- [x] Swagger / OpenAPI with a JWT "Authorize" button (`ServiceExtensions.cs`)
- [x] `.http` files for repeatable API calls (`LearningMap.Api.http`)
- [x] Health check endpoint `/health`
- [x] CORS policy from config (`AllowedOrigins`)

## 4. Dependency Injection & configuration
- [x] Registering services: `AddScoped` / `AddSingleton` / `AddTransient`, and when to use each
- [x] Extension methods per layer: `AddApplication()`, `AddInfrastructure()`, `AddApiServices()`
- [x] Constructor injection everywhere (no `new` for services)
- [x] `appsettings.json` → `appsettings.Development.json` → user-secrets → env vars (later wins)
- [x] Options pattern: `services.Configure<JwtSettings>(...)` + `IOptions<T>`
- [x] **Secrets kept out of source control** with `dotnet user-secrets`
- [x] Fail fast at startup on bad config (JWT key length check)
- [x] `TimeProvider` (.NET 8) instead of `DateTime.Now`, so tests can control time

## 5. Clean Architecture (same as SafeSpace.User.Services)
- [x] Four layers: Domain / Application / Infrastructure / Api, and the dependency direction
- [x] Interfaces in Application, implementations in Infrastructure (Dependency Inversion)
- [x] Entities vs DTOs: never return entities (`PasswordHash` and internal ids stay private)
- [x] Public `Guid` ids for clients, internal `int` identity keys for the database

## 6. CQRS with MediatR (SafeSpace's main pattern)
- [x] Commands (change data) vs Queries (read data) in `Commands/` and `Queries/`
- [x] `IRequest<T>` + `IRequestHandler<TRequest,TResponse>`, `mediator.Send(...)`
- [x] Pipeline behavior: `ValidationBehavior` runs validators before every handler
- [x] Thin controllers: they only call `Send`

## 7. Validation & mapping
- [x] FluentValidation: `AbstractValidator<T>`, `RuleFor`, `When`, `Must`, custom messages
- [x] Shared rules through a generic base validator (`EmployeeCommandValidatorBase<T>`)
- [x] Validating an uploaded file: size, extension, and **real content (magic bytes)**
- [x] AutoMapper profiles, `ForMember`, `MapFrom`, `Ignore`, mapping onto an existing object
- [x] `AssertConfigurationIsValid()` test to catch mapping mistakes

## 8. Database: SQL Server + EF Core
- [x] `DbContext`, `DbSet<T>`, connection strings
- [x] Fluent API configuration: keys, max lengths, `date` columns
- [x] Indexes: unique, and **filtered unique** (`WHERE IsDeleted = 0`)
- [x] Code-first **migrations**: `migrations add`, `database update`, `__EFMigrationsHistory` table
- [x] Removing a column with a migration (`RemoveSalary`): EF warns about data loss, and the stored proc is updated in the same migration
- [x] A migration with raw SQL (creates a stored procedure)
- [x] Apply migrations + seed data at startup (`DbInitialiser.cs`)
- [x] Change tracking: tracked entities vs `AsNoTracking()` for reads
- [x] `IQueryable` builds SQL lazily; filtering, `OrderBy`, `Skip`/`Take` **pagination**, `CountAsync`
- [x] Global query filter for **soft delete** (`HasQueryFilter(e => !e.IsDeleted)`)
- [x] Audit columns via a `SaveChangesInterceptor` (SafeSpace overrides `SaveChangesAsync` instead)
- [x] Seeing the SQL EF generates (EF command logging in `appsettings.Development.json`)
- [x] Database **SEQUENCE** + default value SQL for a readable employee ID (`EMP-00001`)
- [x] Gotcha: EF only uses a DB default when the property is at its CLR default (`null`, not `""`)
- [x] **Data migration** (`BackfillMissingEmployeeCodes`) to repair existing rows

## 9. Repository + Unit of Work (SafeSpace pattern)
- [x] `IGenericRepository<T>` + specific repositories (`IEmployeeRepository`)
- [x] `IUnitOfWork` with one `SaveChangesAsync` = one transaction across repositories

## 10. Dapper + stored procedures (SafeSpace.SeniorLiving.Api's DataAccess style)
- [x] Calling a stored procedure with Dapper (`ReportRepository`, `usp_GetDepartmentSummary`)
- [x] Mapping result columns to a DTO
- [x] When to use EF Core (CRUD) vs Dapper (reports, hand-tuned SQL)

## 11. Authentication & security
- [x] Password hashing with **Argon2id** + random salt (same parameters as SafeSpace `AuthService`)
- [x] Constant-time hash comparison (`CryptographicOperations.FixedTimeEquals`)
- [x] Same error for "unknown email" and "wrong password" (no user enumeration)
- [x] **JWT**: claims (`sub`, `email`, `name`, `role`, `jti`), signing (HMAC-SHA256), expiry
- [x] JWT validation: issuer, audience, lifetime, signing key, clock skew; `MapInboundClaims = false`
- [x] Custom JSON 401/403 responses via `JwtBearerEvents` (like SafeSpace's `OnChallenge`)
- [x] Authorization: `[Authorize]` (any logged-in user) and `[AllowAnonymous]`. Roles (Admin/Viewer) are stored in the JWT; `[Authorize(Roles = "Admin")]` can restrict an endpoint again
- [x] Rate limiting the login endpoint (5 requests/min/IP, built into .NET)
- [x] Request size limits for uploads (`[RequestSizeLimit]`)
- [x] Never trusting client `Content-Type`/file name for uploads
- [x] Package version alignment gotcha: `Microsoft.IdentityModel.*` 7.x vs 8.x

## 12. File / image storage: Azure Blob (SafeSpace's `AzureBlobImageStore`)
- [x] `Azure.Storage.Blobs` SDK: `BlobServiceClient` → container → blob
- [x] Azurite emulator locally (`UseDevelopmentStorage=true`)
- [x] Private container + **SAS URLs** (time-limited read links)
- [x] Store the blob **name** in the DB, generate URLs on read
- [x] Upload, replace and delete flow, and the order relative to the DB save (clean up orphans on failure; delete the old file only after commit)
- [x] Multipart/form-data uploads with `IFormFile`
- [x] Separate image-only endpoints (`PUT`/`DELETE /employees/{guid}/image`) and tracking `ImageUploadedDate`

## 12b. Frontend that calls the API (React + Vite, `client/`)
- [x] One API module (`src/api.js`) that adds `Authorization: Bearer <token>` to every call
- [x] Storing the JWT, auto-logout on 401, protected routes and Admin-only routes (`auth.jsx`)
- [x] Sending `FormData` (multipart) with a file from the browser, plus a local image preview
- [x] Showing server validation errors (400 list) under each form field
- [x] Search with debounce, filters, paging driven by query-string parameters
- [x] Vite dev proxy instead of CORS; CORS `AllowedOrigins` for when the UI calls the API directly
- [x] Decoding a JWT in the browser (`My token` page): anyone can read it, only the server can sign it

## 13. Logging & errors
- [x] Serilog (console + rolling file `logs/`), configured from appsettings
- [x] Structured logging: `logger.LogInformation("Employee {EmployeeGuid} created", guid)`
- [x] Request logging middleware (`UseSerilogRequestLogging`)
- [x] TraceId in 500 responses to match a client error with server logs

## 14. Testing (SafeSpace.User.Tests stack)
- [x] xUnit: `[Fact]`, `[Theory]` + `[InlineData]`
- [x] Moq: `Setup`, `Returns/ReturnsAsync/ThrowsAsync`, `Verify(..., Times.Once)`
- [x] FluentAssertions: `.Should().Be(...)`, `BeOfType<T>()`, `ThrowAsync<T>()`
- [x] FluentValidation `TestValidate` helpers
- [x] Testing handlers without a database (mocks), testing real crypto/JWT round-trip
- [x] Arrange / Act / Assert structure

---

## Next up (not done yet)
- [ ] Integration tests with `WebApplicationFactory` + a real test database (Testcontainers)
- [ ] Refresh tokens + logout / token revocation
- [ ] MFA / OTP flow like SafeSpace `LoginCommand` (email/SMS code), using a fake sender
- [ ] Forgot/reset password with expiring one-time tokens
- [ ] Optimistic concurrency (`rowversion`) so two admins can't overwrite each other's edits
- [ ] Multi-tenancy like SafeSpace's `TenantResolutionMiddleware` (one DB per tenant)
- [ ] Redis distributed cache (`IDistributedCache`) for read-heavy endpoints
- [ ] Azure Service Bus / message queues for async work (e.g. image thumbnails)
- [ ] SignalR real-time updates (SafeSpace `Hubs/`)
- [ ] Background jobs (`BackgroundService` / hosted services)
- [ ] API versioning, `ProblemDetails`, minimal APIs (compare with controllers)
- [ ] Docker + CI pipeline (SafeSpace's `Azure-Pipelines.Yml`)
- [ ] Azure Key Vault + Managed Identity instead of connection-string keys
- [ ] Application Insights telemetry
