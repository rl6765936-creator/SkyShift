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

Run with `dotnet test` from the solution root.

## System Description

SkyShift (`עולים לאוויר`) is a REST API for managing a shared, limited fleet of Vehicles across Flights. There are two roles: a **User** claims exactly one Vehicle for a one-month trial (`VehicleAssignment`, via `SelectVehicleUseCase`) and logs `Flight`s against it; a **Manager** manages the Vehicle fleet, views all Flights/Assignments, and reads aggregate activity/compliance stats (`ManagerDashboardController` and the other Manager-only controllers, guarded by `[Authorize(Roles = "Manager")]`). Vehicles are the competed-over, limited resource: only one User may hold a given Vehicle at a time (`Vehicle.Assign()` only succeeds while `Status == Available`), which is exactly why the concurrency handling below exists. Authentication is JWT Bearer (`AuthController`), with the Role claim issued at login/register gating the Manager endpoints.

## Limited Resource & Concurrency

`SelectVehicleUseCase` is the one place two requests can race for the same Vehicle. Two independent failure modes are both translated to the existing `VehicleNotAvailableException` (HTTP 409) inside `UnitOfWork.SaveChangesAsync()`, so Application code never has to know EF Core's or SQL Server's own exception types:

1. **Two different Users select the same Vehicle at the same moment.** `Vehicle` carries a shadow `RowVersion` optimistic-concurrency token (`VehicleConfiguration`). Whichever request's `SaveChangesAsync()` commits first wins; the second gets a `DbUpdateConcurrencyException`, which `UnitOfWork` catches, reads the Vehicle's current status, and re-throws as `VehicleNotAvailableException`.
2. **The same User sends two concurrent `SelectVehicle` requests.** A `RowVersion` conflict alone would not catch this (the two requests could even target different Vehicles). This case is guarded by a Serializable transaction opened on first `DbContext` use in the request scope; the losing request gets a SQL Server serialization failure (error 1205 deadlock, or 1222 lock timeout), which `UnitOfWork` also catches and translates to the same `VehicleNotAvailableException`.

`tests/SkyShift.Tests/Concurrency/VehicleConcurrencyTests.cs` proves case 1 directly against a real EF Core provider (`Microsoft.EntityFrameworkCore.InMemory`): two independent `DbContext` instances load the same Vehicle, both modify it, the first `SaveChangesAsync` succeeds and the second throws `DbUpdateConcurrencyException`.

## Running the project

1. **Configure secrets** — `ConnectionStrings:SkyShiftDb` and `Jwt:SigningKey` are intentionally left blank in `appsettings.json`/`appsettings.Development.json` (not committed to the repo). Set them yourself via `dotnet user-secrets` or a local, untracked `appsettings.Development.json` override: a SQL Server/LocalDB connection string, and any signing key of your choosing.
2. **Restore and build**: `dotnet restore` then `dotnet build` from the solution root (or Build → Rebuild Solution in Visual Studio).
3. **Apply migrations**: `dotnet ef database update --project src/SkyShift.Infrastructure --startup-project src/SkyShift.Api`.
4. **Run**: `dotnet run --project src/SkyShift.Api` (or F5 in Visual Studio). Swagger opens automatically at `https://localhost:7080/swagger` (see `src/SkyShift.Api/Properties/launchSettings.json`). The two demo accounts above are seeded automatically on first startup — no manual steps needed to log in.
5. **Run tests**: `dotnet test` from the solution root (see "Tests" above).
