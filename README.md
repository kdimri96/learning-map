# learning-map

A small .NET 8 Web API for learning backend development **the way SafeSpace.SeniorLiving is built**:
login with password (JWT), plus Employee CRUD with a profile image.

Everything runs locally and is free: SQL Server Express, Azurite (local Azure Blob Storage), no SMS/email/paid services.

---

## 1. Tools (one-time install)

| Tool | Why | Install |
|---|---|---|
| .NET 8 SDK | Builds and runs C# (same `net8.0` as SafeSpace) | `winget install Microsoft.DotNet.SDK.8` |
| SQL Server 2022 Express | The database (same engine as SafeSpace, free) | `winget install Microsoft.SQLServer.2022.Express` |
| `dotnet-ef` | Creates/applies EF Core migrations | `dotnet tool install --global dotnet-ef --version 9.0.4` |
| Azurite | Local Azure Blob Storage emulator for images | `npm install -g azurite` |
| VS Code extensions | C# support, SQL GUI (pgAdmin-like), `.http` runner | `ms-dotnettools.csdevkit`, `ms-mssql.mssql`, `humao.rest-client` |

Check: `dotnet --list-sdks` should show `8.0.x`.

## 2. Secrets (one-time)

Secrets are **not** in `appsettings.json`. They live in your Windows user profile via *user-secrets*:

```powershell
cd src/LearningMap.Api
dotnet user-secrets set "JwtSettings:IssuerSigningKey" "<any random string of 32+ characters>"
dotnet user-secrets set "SeedAdmin:Password" "Admin#12345"
dotnet user-secrets list
```

## 3. Run it

Terminal 1, blob storage:

```powershell
azurite-blob --location .azurite --skipApiVersionCheck
```

Terminal 2, the API:

```powershell
dotnet run --project src/LearningMap.Api --launch-profile https
```

Terminal 3, the React UI:

```powershell
cd client
npm install      # first time only
npm run dev
```

**Open the UI at http://localhost:5173** and log in with `admin@learningmap.local` / your SeedAdmin password.

On first start (Development only) the API **creates the database, applies migrations, creates the
blob container and seeds the admin user**. You can also open:

- Swagger UI: https://localhost:7080/swagger
- Health: https://localhost:7080/health

Swagger walkthrough:
1. `POST /auth/login` with `admin@learningmap.local` / your SeedAdmin password, then copy `data.token`.
2. Click **Authorize** (top right), paste the token.
3. Try `POST /employees` (pick an image), `GET /employees`, `PUT`, `DELETE`.
4. Open `data.imageUrl` in a browser to see the uploaded picture (a time-limited SAS link).

Or use [`src/LearningMap.Api/LearningMap.Api.http`](src/LearningMap.Api/LearningMap.Api.http) with the REST Client extension. It logs in and reuses the token automatically.

## 4. Tests

```powershell
dotnet test
```

## 5. Look at the database

In VS Code: SQL Server extension → Add Connection → server `localhost\SQLEXPRESS`,
authentication **Windows**, database `LearningMapDb`, trust server certificate **yes**.
Tables: `Users`, `Employees`, `__EFMigrationsHistory`. Stored procedure: `usp_GetDepartmentSummary`.

## 6. Endpoints

| Method | Route | Who | What |
|---|---|---|---|
| POST | `/auth/login` | anyone (5/min per IP) | email + password → JWT |
| POST | `/auth/register` | anyone | creates an account (can then add/edit/delete) |
| GET | `/auth/me` | logged in | shows your token's claims |
| GET | `/employees?search=&department=&pageNumber=&pageSize=` | logged in | paged list (search also matches the ID `EMP-00001`) |
| GET | `/employees/{guid}` | logged in | one employee: ID, details, image URL, image upload date |
| PUT | `/employees/{guid}/image` (multipart `Image`) | logged in | upload/replace only the image |
| DELETE | `/employees/{guid}/image` | logged in | remove only the image |
| GET | `/employees/department-summary` | logged in | Dapper + stored procedure |
| POST | `/employees` (multipart) | logged in | create, optional image |
| PUT | `/employees/{guid}` (multipart) | logged in | update, new image or `RemoveImage=true` |
| DELETE | `/employees/{guid}` | logged in | soft delete + remove image |

## 7. How a request flows

```
HTTP request
  → Middleware: Serilog request log → HTTPS → CORS → RateLimiter → Authentication (JWT) → Authorization ([Authorize])
  → Controller (thin)          EmployeesController.Create
  → MediatR.Send(command)
      → ValidationBehavior     runs CreateEmployeeCommandValidator (FluentValidation)
      → Handler                CreateEmployeeCommandHandler (business logic)
          → IImageStore        AzureBlobImageStore → Azurite
          → IUnitOfWork        EmployeeRepository → EF Core → SQL Server
              → AuditableEntityInterceptor stamps CreatedBy/CreatedDate
  ← ApiResponseData { responseStatus, errorMessage, statusCode, data }
  (any exception → GlobalExceptionFilter → 400/500 in the same envelope)
```

## 8. Project layout (Clean Architecture, same as SafeSpace.User.Services)

```
src/
  LearningMap.Domain/          Entities (AppUser, Employee), settings classes, constants. Depends on nothing.
  LearningMap.Application/     Commands/, Queries/ (MediatR), validators, AutoMapper profile, INTERFACES.
  LearningMap.Infrastructure/  EF Core DbContext + migrations, repositories, Unit of Work, Dapper,
                               Argon2 + JWT (AuthService), Azure Blob (AzureBlobImageStore).
  LearningMap.Api/             Program.cs, controllers, filters, JWT/Swagger/CORS/rate-limit setup.
tests/
  LearningMap.Tests/           xUnit + Moq + FluentAssertions.
client/                        React + Vite UI (src/api.js = every API call; src/pages = screens).
                               Vite proxies /api/* → https://localhost:7080, so no CORS during development.
```

Dependency direction: `Api → Application ← Infrastructure`, everything → `Domain`.

## 9. Useful EF Core commands

```powershell
# after changing an entity or configuration:
dotnet ef migrations add <Name> --project src/LearningMap.Infrastructure --startup-project src/LearningMap.Api --output-dir Data/Migrations
dotnet ef database update     --project src/LearningMap.Infrastructure --startup-project src/LearningMap.Api
dotnet ef migrations script   --project src/LearningMap.Infrastructure --startup-project src/LearningMap.Api   # see the SQL
```

See [LEARNING-CHECKLIST.md](LEARNING-CHECKLIST.md) for everything this project covers.
