# SkyShift — Server (`עולים לאוויר`)

## Demo accounts

Two demo accounts are seeded automatically, once, the first time the API starts against an up-to-date database (see "Seeding" below). Use them to log in via `POST /api/auth/login` immediately after running the project — no manual database steps are required.

| Role    | Email                  | Password        |
|---------|-------------------------|------------------|
| User    | `user@skyshift.demo`    | `User@12345`     |
| Manager | `manager@skyshift.demo` | `Manager@12345`  |

These are fixed, intentionally-public demo credentials (not secrets) — they exist only to make the system runnable and demonstrable right after setup.

## Seeding

On every startup, the API checks for these two accounts by email and creates whichever are missing (`SeedDemoUsersUseCase`, invoked once in `Program.cs` after the app is built). It is safe to run repeatedly: existing accounts are left untouched, and no duplicates are ever created. No Vehicles, Flights, or VehicleAssignments are seeded — those are created through the Manager API and the normal User flow, by design.

## Tests

`tests/SkyShift.Tests` (xUnit + Moq) covers:

- `SelectVehicleUseCaseTests` — the limited-resource (Vehicle) logic at the Service layer, with repositories mocked via Moq: the valid case (vehicle available -> assigned) and the rejected case (vehicle already taken -> `VehicleNotAvailableException`).
- `VehicleConcurrencyTests` — the required proof of the optimistic-concurrency mechanism itself: two independent `DbContext` instances load the same Vehicle, both modify it, the first `SaveChangesAsync` succeeds, the second throws `DbUpdateConcurrencyException`. Uses EF Core's InMemory provider so it needs no real database.

- `ManagerDashboardQueryTests`, `ReadOnlyVehicleQueryTests`, `DeleteVehicleTests`, `RoutesAndLocationTests`, `ExceptionClassificationTests` — dashboard aggregation without per-assignment queries, untracked display reads, vehicle deletion rules, `Location` targets and authorization metadata, and the error/log-level contract.

Run with `dotnet test` from the solution root. Tests marked `[SqlServerFact]` (category `SqlServer`) run only when `SKYSHIFT_TEST_SQLSERVER` holds a connection string to a test SQL Server; each creates and drops its own `SkyShiftTests_*` database and never touches the application database.

## System Description

SkyShift (`עולים לאוויר`) is a REST API for managing a shared, limited fleet of Vehicles across Flights. There are two roles: a **User** claims exactly one Vehicle for a one-month trial (`VehicleAssignment`, via `SelectVehicleUseCase`) and logs `Flight`s against it; a **Manager** manages the Vehicle fleet, views all Flights/Assignments, and reads aggregate activity/compliance stats (`ManagerDashboardController` and the other Manager-only controllers, guarded by `[Authorize(Roles = "Manager")]`). Vehicles are the competed-over, limited resource: only one User may hold a given Vehicle at a time (`Vehicle.Assign()` only succeeds while `Status == Available`), which is exactly why the concurrency handling below exists. Authentication is JWT Bearer (`AuthController`), with the Role claim issued at login/register gating the Manager endpoints.

## Architecture

Clean Architecture in four projects. Dependencies point inward only; the Domain depends on nothing.

```
SkyShift.Api ──────────────► SkyShift.Application ──► SkyShift.Domain
     │                              ▲
     └──► SkyShift.Infrastructure ──┘ (also ► Domain)
```

| Project | Contains | References |
|---|---|---|
| `SkyShift.Domain` | Entities with their invariants (`Vehicle` status transitions, `VehicleAssignment` trial window, `VehicleCatalogItem` validation), enums, domain exceptions, `LotteryEligibilityService` | nothing (no packages) |
| `SkyShift.Application` | One class per use case (`UseCases/*`), request/response DTOs, AutoMapper profile, and the abstractions the use cases need (`IUserRepository`, `IVehicleRepository`, `IVehicleAssignmentRepository`, `IFlightRepository`, `IVehicleCatalogRepository`, `IUnitOfWork`, `IPasswordHasher`) | Domain, AutoMapper |
| `SkyShift.Infrastructure` | EF Core `SkyShiftDbContext`, entity configurations, migrations, repositories, `UnitOfWork` (transactions and translation of SQL errors into domain exceptions), `PasswordHasher` (ASP.NET Core Identity's PBKDF2), `AddInfrastructure` | Application (to implement its abstractions), Domain, EF Core SQL Server, Identity.Core |
| `SkyShift.Api` | Thin controllers (HTTP binding, claims, status codes; they call use cases only), `SkyShiftExceptionHandler` (ProblemDetails + bilingual messages), `CorrelationIdMiddleware`, JWT issuing/validation, Swagger, NLog, and the composition root `Program.cs` | Application; Infrastructure only for `AddInfrastructure` in `Program.cs` |

Design choices:

- **Interface ownership.** Interfaces are owned by Application, the layer that uses them, and implemented in Infrastructure, so Infrastructure references Application. This is the standard Clean Architecture dependency inversion.
- **Use cases as concrete classes.** Each use case is a plain class injected directly into controllers, with no mediator library.
- **No EF types in the Domain.** The optimistic-concurrency token `RowVersion` is an EF shadow property configured in Infrastructure, so the Domain stays free of persistence types.
- **Query rules owned by the Domain.** Rules that repositories must express as queries are exposed by the Domain as plain expression trees (`VehicleAssignment.ActiveAt` / `EndedBefore`) instead of being restated in LINQ.
- **Lifetimes.** `DbContext`, repositories, `UnitOfWork` and use cases are scoped. Singletons (`JwtTokenService`, `SkyShiftExceptionHandler`, `LotteryEligibilityService`, `TimeProvider`) hold no scoped dependencies.
- **Supporting projects.** `tests/SkyShift.Tests` references all projects plus xUnit, Moq and EF InMemory. `tools/SkyShift.DemoReset` is a standalone local tool, not part of `SkyShift.sln`; it references Application and Infrastructure, and no production project references it.

## Limited Resource & Concurrency

`SelectVehicleUseCase` is the one place two requests can race for the same Vehicle. Two independent failure modes are both translated to the existing `VehicleNotAvailableException` (HTTP 409) inside `UnitOfWork.SaveChangesAsync()`, so Application code never has to know EF Core's or SQL Server's own exception types:

1. **Two different Users select the same Vehicle at the same moment.** `Vehicle` carries a shadow `RowVersion` optimistic-concurrency token (`VehicleConfiguration`). Whichever request's `SaveChangesAsync()` commits first wins; the second gets a `DbUpdateConcurrencyException`, which `UnitOfWork` catches, reads the Vehicle's current status, and re-throws as `VehicleNotAvailableException`.
2. **The same User sends two concurrent `SelectVehicle` requests.** A `RowVersion` conflict alone would not catch this (the two requests could even target different Vehicles). This case is guarded by a Serializable transaction opened on first `DbContext` use in the request scope; the losing request gets a SQL Server serialization failure (error 1205 deadlock, or 1222 lock timeout), which `UnitOfWork` also catches and translates to the same `VehicleNotAvailableException`.

`tests/SkyShift.Tests/Concurrency/VehicleConcurrencyTests.cs` proves case 1 directly against a real EF Core provider (`Microsoft.EntityFrameworkCore.InMemory`): two independent `DbContext` instances load the same Vehicle, both modify it, the first `SaveChangesAsync` succeeds and the second throws `DbUpdateConcurrencyException`.

Deleting a fleet Vehicle (`DELETE /api/manager/vehicles/{id}`) uses the same protection: the Vehicle is read inside the same Serializable transaction, the history check runs in that transaction, and the `DELETE` carries the `RowVersion`. A delete racing an assignment of the same Vehicle therefore has exactly one winner; the loser gets a 409 (`vehicle_delete_conflict` or `vehicle_not_available`).

## API notes

- **Assignments:** `POST /api/vehicles/{id}/assignments` creates the caller's one-month assignment of vehicle `{id}` (204). `POST /api/vehicles/select` with `{ "vehicleId": n }` is kept as a compatibility alias for the current React client; both use `SelectVehicleUseCase` and return the same responses.
- **Created resources:** `POST /api/auth/register` returns `201` with `Location: /api/users/{id}` (`GET` — the caller's own profile, or any profile for a Manager; other users' ids return 404). `POST /api/manager/vehicles` returns `201` with `Location: /api/manager/vehicles/{id}` (`GET`, Manager only).
- **Deleting a vehicle:** `DELETE /api/manager/vehicles/{id}` (Manager only) deletes only an `Available` vehicle that has never had an assignment or a flight: `204`; `404` if it does not exist; `409` with a bilingual code otherwise (`vehicle_delete_not_available`, `vehicle_delete_has_history`, `vehicle_delete_conflict`). Vehicles with history are kept — retire them instead; no history is ever cascaded. A catalog model counts as used exactly while a Vehicle row references it, so deleting an unused vehicle returns its catalog model to the list available for fleet creation.
- **Errors:** expected outcomes return ProblemDetails with `code`, `messageHe` and `messageEn` (`detail` is Hebrew when `Accept-Language` is Hebrew). Any unexpected exception is a generic `500`. Logs: validation/not-found at Information, conflicts and rejected logins at Warning, unexpected failures at Error — each with the request's CorrelationId.

## Running the project

1. **Configure secrets** — `ConnectionStrings:SkyShiftDb` and `Jwt:SigningKey` are intentionally left blank in `appsettings.json`/`appsettings.Development.json` (not committed to the repo). Set them yourself via `dotnet user-secrets` or a local, untracked `appsettings.Development.json` override: a SQL Server/LocalDB connection string, and any signing key of your choosing.
2. **Restore and build**: `dotnet restore` then `dotnet build` from the solution root (or Build → Rebuild Solution in Visual Studio).
3. **Apply migrations**: `dotnet ef database update --project src/SkyShift.Infrastructure --startup-project src/SkyShift.Api`.
4. **Run**: `dotnet run --project src/SkyShift.Api` (or F5 in Visual Studio). Swagger opens automatically at `https://localhost:7080/swagger` (see `src/SkyShift.Api/Properties/launchSettings.json`). The two demo accounts above are seeded automatically on first startup — no manual steps needed to log in.
5. **Run tests**: `dotnet test` from the solution root (see "Tests" above).

## Deploying (Docker / Render)

The root `Dockerfile` builds and publishes `SkyShift.Api` on .NET 8 and runs it as a non-root user in `Production`. On Render, create a **Web Service** with runtime **Docker** (Dockerfile path `./Dockerfile`, build context `.`). The API listens on Render's `PORT` (see `Hosting/PlatformPort.cs`); Render terminates HTTPS. CORS allows `https://skyshift-react.onrender.com` via `Cors:AllowedOrigins` in `appsettings.json`.

Settings come only from environment variables (ASP.NET Core maps `__` to `:`); never put their values in files:

- `ConnectionStrings__SkyShiftDb` — SQL Server connection string (secret).
- `Jwt__SigningKey` — random key of at least 32 bytes (secret).
- `Jwt__Issuer`, `Jwt__Audience` — token issuer/audience names (required, not secret).
- Optional: `Jwt__ExpirationMinutes` (default 60), `AutoMapper__LicenseKey`, `Cors__AllowedOrigins__0` to override the allowed origin.

The container does not apply migrations: run `dotnet ef database update` against the target database yourself before the first start. Demo seeding (see "Seeding") runs on every startup in every environment, so the deployed demo has the two public demo accounts.
