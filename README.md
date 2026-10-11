# SkyShift — Server (`עולים לאוויר`)

## Demo accounts

Two demo accounts are seeded automatically, once, the first time the API starts against an up-to-date database (see "Seeding" below). Use them to log in via `POST /api/auth/login` immediately after running the project — no manual database steps are required.

| Role    | Email                  | Password        |
|---------|-------------------------|------------------|
| User    | `user@skyshift.demo`    | `User@12345`     |
| Manager | `manager@skyshift.demo` | `Manager@12345`  |

These are fixed, intentionally-public demo credentials (not secrets) — they exist only to make the system runnable and demonstrable right after setup.

## Seeding

On every startup, in every environment and with either database provider, the API checks for these two accounts by email and creates whichever are missing (`SeedDemoUsersUseCase`, invoked once in `Program.cs` after the app is built). This is intentional: the public demo deployment must offer the documented accounts. It is safe to run repeatedly: existing accounts are left untouched, and no duplicates are ever created. No Vehicles, Flights, or VehicleAssignments are seeded — those are created through the Manager API and the normal User flow, by design.

## Tests

`tests/SkyShift.Tests` (xUnit + Moq) covers:

- `SelectVehicleUseCaseTests` — the limited-resource (Vehicle) logic at the Service layer, with repositories mocked via Moq: the valid case (vehicle available -> assigned) and the rejected case (vehicle already taken -> `VehicleNotAvailableException`).
- `VehicleConcurrencyTests` — the required proof of the optimistic-concurrency mechanism itself: two independent `DbContext` instances load the same Vehicle, both modify it, the first `SaveChangesAsync` succeeds, the second throws `DbUpdateConcurrencyException`. Uses EF Core's InMemory provider so it needs no real database.

- `ManagerDashboardQueryTests`, `ReadOnlyVehicleQueryTests`, `DeleteVehicleTests`, `RoutesAndLocationTests`, `ExceptionClassificationTests` — dashboard aggregation without per-assignment queries, untracked display reads, vehicle deletion rules, `Location` targets and authorization metadata, and the error/log-level contract.

- `PostgreSql/ProviderModelTests` — no database needed: each provider's model matches its own migrations (the SQL Server model is unchanged by PostgreSQL support) and `Database:Provider` is parsed explicitly.
- `PostgreSql/PostgreSqlTests` — real PostgreSQL: migrations and the 22 catalog items, both winners of a concurrent assignment/deletion, concurrent assignments, duplicate registrations (any letter case), duplicate catalog creation, duplicate flights, UTC dates, rollback consistency and the `xmin` concurrency token.

Run with `dotnet test` from the solution root. Database tests are skipped unless their variable is set, and each one creates and drops its own temporary database; none touches the application database:

- `[SqlServerFact]` (category `SqlServer`): `SKYSHIFT_TEST_SQLSERVER` = a connection string to a test SQL Server **without a database name** (databases `SkyShiftTests_*`).
- `[PostgreSqlFact]` (category `PostgreSql`): `SKYSHIFT_TEST_POSTGRES` = a connection string to a test PostgreSQL server **without a database name**, with permission to create databases (databases `skyshift_tests_*`). Never point it at the deployed Neon database.

## System Description

SkyShift (`עולים לאוויר`) is a REST API for managing a shared, limited fleet of Vehicles across Flights. There are two roles: a **User** claims exactly one Vehicle for a one-month trial (`VehicleAssignment`, via `SelectVehicleUseCase`) and logs `Flight`s against it; a **Manager** manages the Vehicle fleet, views all Flights/Assignments, and reads aggregate activity/compliance stats (`ManagerDashboardController` and the other Manager-only controllers, guarded by `[Authorize(Roles = "Manager")]`). Vehicles are the competed-over, limited resource: only one User may hold a given Vehicle at a time (`Vehicle.Assign()` only succeeds while `Status == Available`), which is exactly why the concurrency handling below exists. Authentication is JWT Bearer (`AuthController`), with the Role claim issued at login/register gating the Manager endpoints.

## Architecture

Clean Architecture in four projects, plus a migrations-only project for PostgreSQL. Dependencies point inward only; the Domain depends on nothing.

```
SkyShift.Api ──────────────► SkyShift.Application ──► SkyShift.Domain
     │                              ▲
     ├──► SkyShift.Infrastructure ──┘ (also ► Domain)
     └──► SkyShift.Infrastructure.PostgreSql (PostgreSQL migrations only) ──► SkyShift.Infrastructure
```

| Project | Contains | References |
|---|---|---|
| `SkyShift.Domain` | Entities with their invariants (`Vehicle` status transitions, `VehicleAssignment` trial window, `VehicleCatalogItem` validation), enums, domain exceptions, `LotteryEligibilityService` | nothing (no packages) |
| `SkyShift.Application` | One class per use case (`UseCases/*`), request/response DTOs, AutoMapper profile, and the abstractions the use cases need (`IUserRepository`, `IVehicleRepository`, `IVehicleAssignmentRepository`, `IFlightRepository`, `IVehicleCatalogRepository`, `IUnitOfWork`, `IPasswordHasher`) | Domain, AutoMapper |
| `SkyShift.Infrastructure` | EF Core `SkyShiftDbContext`, entity configurations (plus `PostgreSqlModel` for PostgreSQL-only mapping), SQL Server migrations, repositories, `UnitOfWork` (transactions and translation of SQL Server/PostgreSQL errors into domain exceptions), `PasswordHasher` (ASP.NET Core Identity's PBKDF2), `AddInfrastructure` with explicit provider selection | Application (to implement its abstractions), Domain, EF Core SQL Server, Npgsql EF Core provider, Identity.Core |
| `SkyShift.Infrastructure.PostgreSql` | PostgreSQL migrations only (`Migrations/*_InitialPostgreSql`); EF Core requires a separate migrations set per provider | Infrastructure |
| `SkyShift.Api` | Thin controllers (HTTP binding, claims, status codes; they call use cases only), `SkyShiftExceptionHandler` (ProblemDetails + bilingual messages), `CorrelationIdMiddleware`, JWT issuing/validation, Swagger, NLog, and the composition root `Program.cs` | Application; Infrastructure only for `AddInfrastructure` in `Program.cs`; Infrastructure.PostgreSql only so its migrations are deployed |

Design choices:

- **Interface ownership.** Interfaces are owned by Application, the layer that uses them, and implemented in Infrastructure, so Infrastructure references Application. This is the standard Clean Architecture dependency inversion.
- **Use cases as concrete classes.** Each use case is a plain class injected directly into controllers, with no mediator library.
- **No EF types in the Domain.** The optimistic-concurrency token is an EF shadow property configured in Infrastructure (`SkyShiftDbContext`): `RowVersion` (rowversion) on SQL Server, `xmin` on PostgreSQL. The Domain stays free of persistence types.
- **Two database providers.** `Database:Provider` selects `SqlServer` (default when unset) or `PostgreSQL`; any other value stops startup. Both use the same DbContext, repositories and business rules; only mapping details, locking SQL and error codes differ, and each provider has its own migrations.
- **Query rules owned by the Domain.** Rules that repositories must express as queries are exposed by the Domain as plain expression trees (`VehicleAssignment.ActiveAt` / `EndedBefore`) instead of being restated in LINQ.
- **Lifetimes.** `DbContext`, repositories, `UnitOfWork` and use cases are scoped. Singletons (`JwtTokenService`, `SkyShiftExceptionHandler`, `LotteryEligibilityService`, `TimeProvider`) hold no scoped dependencies.
- **Supporting projects.** `tests/SkyShift.Tests` references all projects plus xUnit, Moq and EF InMemory. `tools/SkyShift.DemoReset` is a standalone local tool, not part of `SkyShift.sln`; it references Application and Infrastructure, and no production project references it.

## Limited Resource & Concurrency

`SelectVehicleUseCase` is the one place two requests can race for the same Vehicle. Two independent failure modes are both translated to the existing `VehicleNotAvailableException` (HTTP 409) inside `UnitOfWork.SaveChangesAsync()`, so Application code never has to know EF Core's or SQL Server's own exception types:

1. **Two different Users select the same Vehicle at the same moment.** `Vehicle` carries a shadow optimistic-concurrency token (`RowVersion` on SQL Server, `xmin` on PostgreSQL; configured in `SkyShiftDbContext`). Whichever request's `SaveChangesAsync()` commits first wins; the second gets a `DbUpdateConcurrencyException`, which `UnitOfWork` catches, reads the Vehicle's current status, and re-throws as `VehicleNotAvailableException`.
2. **The same User sends two concurrent `SelectVehicle` requests.** A `RowVersion` conflict alone would not catch this (the two requests could even target different Vehicles). This case is guarded by a Serializable transaction opened on first `DbContext` use in the request scope; the losing request gets a SQL Server serialization failure (error 1205 deadlock, or 1222 lock timeout), which `UnitOfWork` also catches and translates to the same `VehicleNotAvailableException`.

`tests/SkyShift.Tests/Concurrency/VehicleConcurrencyTests.cs` proves case 1 directly against a real EF Core provider (`Microsoft.EntityFrameworkCore.InMemory`): two independent `DbContext` instances load the same Vehicle, both modify it, the first `SaveChangesAsync` succeeds and the second throws `DbUpdateConcurrencyException`.

**PostgreSQL.** The same paths run at Read Committed with explicit row locks instead of Serializable: the Vehicle is read with `SELECT … FOR UPDATE` and the user's row is locked with `FOR NO KEY UPDATE` before the active-assignment check. A waiting request then reads the winner's committed row, so the outcomes match SQL Server (404/409, never a raw database error). PostgreSQL's Serializable would instead abort the waiting request with error 40001. `UnitOfWork` maps PostgreSQL 23505 / 23503 / 40001 / 40P01 exactly like SQL Server 2601-2627 / 547 / 1205-1222. Dates are stored as `timestamptz` in UTC, and `Users.Email` uses a case-insensitive collation so duplicate-email protection and login match SQL Server.

Deleting a fleet Vehicle (`DELETE /api/manager/vehicles/{id}`) uses the same protection: the Vehicle is read inside the same Serializable transaction, the history check runs in that transaction, and the `DELETE` carries the `RowVersion`. A delete racing an assignment of the same Vehicle therefore has exactly one winner; the loser gets a 409 (`vehicle_delete_conflict` or `vehicle_not_available`).

## API notes

- **Assignments:** `POST /api/vehicles/{id}/assignments` creates the caller's one-month assignment of vehicle `{id}` (204). `POST /api/vehicles/select` with `{ "vehicleId": n }` is kept as a compatibility alias for the current React client; both use `SelectVehicleUseCase` and return the same responses.
- **Created resources:** `POST /api/auth/register` returns `201` with `Location: /api/users/{id}` (`GET` — the caller's own profile, or any profile for a Manager; other users' ids return 404). `POST /api/manager/vehicles` returns `201` with `Location: /api/manager/vehicles/{id}` (`GET`, Manager only).
- **Deleting a vehicle:** `DELETE /api/manager/vehicles/{id}` (Manager only) deletes only an `Available` vehicle that has never had an assignment or a flight: `204`; `404` if it does not exist; `409` with a bilingual code otherwise (`vehicle_delete_not_available`, `vehicle_delete_has_history`, `vehicle_delete_conflict`). Vehicles with history are kept — retire them instead; no history is ever cascaded. A catalog model counts as used exactly while a Vehicle row references it, so deleting an unused vehicle returns its catalog model to the list available for fleet creation.
- **Errors:** expected outcomes return ProblemDetails with `code`, `messageHe` and `messageEn` (`detail` is Hebrew when `Accept-Language` is Hebrew). Any unexpected exception is a generic `500`. Logs: validation/not-found at Information, conflicts and rejected logins at Warning, unexpected failures at Error — each with the request's CorrelationId.

## Running the project

1. **Configure secrets** — `ConnectionStrings:SkyShiftDb` and `Jwt:SigningKey` are intentionally left blank in `appsettings.json`/`appsettings.Development.json` (not committed to the repo). Set them yourself via `dotnet user-secrets` or a local, untracked `appsettings.Development.json` override: a SQL Server/LocalDB connection string, and any signing key of your choosing.
2. **Restore and build**: `dotnet restore` then `dotnet build` from the solution root (or Build → Rebuild Solution in Visual Studio).
3. **Apply migrations**: `dotnet ef database update --project src/SkyShift.Infrastructure --startup-project src/SkyShift.Api` (SQL Server, the default provider; see "PostgreSQL / Neon" below for PostgreSQL).
4. **Run**: `dotnet run --project src/SkyShift.Api` (or F5 in Visual Studio). Swagger opens automatically at `https://localhost:7080/swagger` (see `src/SkyShift.Api/Properties/launchSettings.json`). The two demo accounts above are seeded automatically on first startup — no manual steps needed to log in.
5. **Run tests**: `dotnet test` from the solution root (see "Tests" above).

## Deploying (Docker / Render)

The root `Dockerfile` builds and publishes `SkyShift.Api` on .NET 8 and runs it as a non-root user in `Production`. On Render, create a **Web Service** with runtime **Docker** (Dockerfile path `./Dockerfile`, build context `.`). The API listens on Render's `PORT` (see `Hosting/PlatformPort.cs`); Render terminates HTTPS. CORS allows `https://skyshift-react.onrender.com` via `Cors:AllowedOrigins` in `appsettings.json`.

Settings come only from environment variables (ASP.NET Core maps `__` to `:`); never put their values in files:

- `Database__Provider` — `PostgreSQL` for Neon (unset or `SqlServer` means SQL Server).
- `ConnectionStrings__SkyShiftDb` — connection string for that provider (secret).
- `Jwt__SigningKey` — random key of at least 32 bytes (secret).
- `Jwt__Issuer`, `Jwt__Audience` — token issuer/audience names (required, not secret).
- Optional: `Jwt__ExpirationMinutes` (default 60), `AutoMapper__LicenseKey`, `Cors__AllowedOrigins__0` to override the allowed origin.

The container does not apply migrations: create the schema yourself before the first start (see below). Demo seeding (see "Seeding") runs on every startup in every environment, so the deployed demo has the two public demo accounts.

### PostgreSQL / Neon

The full, step-by-step procedure (with checks before anything is changed) is in `PROJECT-HANDOFF.md`. In short:

1. **Connection string.** Npgsql needs key/value format, not the `postgresql://` URL. Use Neon's **direct** host (no `-pooler`) for migrations and for the app:
   `Host=<ep-...>.eu-central-1.aws.neon.tech;Database=<db>;Username=<role>;Password=<password>;SSL Mode=Require;Maximum Pool Size=10;Connection Idle Lifetime=60`
   Never put it in a file, a commit or a `VITE_*` variable.
2. **Inspect first (read-only).** In the Neon SQL Editor, list existing tables; continue only if none of the SkyShift tables or `__EFMigrationsHistory` exist.
3. **Review the SQL** (no connection needed): with `Database__Provider=PostgreSQL` and any placeholder connection string,
   `dotnet ef migrations script --idempotent --project src/SkyShift.Infrastructure.PostgreSql --startup-project src/SkyShift.Api -o neon-schema.sql`.
   It only creates the collation, 5 tables, their indexes and the 22 catalog rows. Do not commit the file.
4. **Apply** with `dotnet ef database update --project src/SkyShift.Infrastructure.PostgreSql --startup-project src/SkyShift.Api`, reading the connection string with masked input. `dotnet ef` stops the API before it starts, so no demo users are seeded and no JWT settings are needed. Never run the SQL Server project's migrations (`--project src/SkyShift.Infrastructure`) against PostgreSQL.
5. **Render:** `Database__Provider=PostgreSQL`, `ConnectionStrings__SkyShiftDb`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey`. The demo accounts are created on the first start.

### Frontend (Render Static Site)

`src/api.ts` calls `${VITE_API_BASE_URL}/api/...`, so set `VITE_API_BASE_URL` to the API origin only, without `/api` and without a trailing slash (for example `https://<your-api>.onrender.com`). Vite inlines it at build time, so redeploy with "Clear build cache & deploy" after changing it. Keep the rewrite rule `/*` → `/index.html` (action Rewrite) so refreshing a client route works.
