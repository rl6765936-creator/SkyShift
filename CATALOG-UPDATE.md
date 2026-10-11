# SkyShift backend — vehicle catalog, error codes, verification (2026-10-10)

Backend-only update. No frontend files were changed. Nothing has been applied to
`SkyShiftDb`. Nothing has been committed to git.

## 0. Update 2 (2026-10-11): complete catalog specifications

- Every catalog item now has **seats**, a **maximum road speed** and short **EN/HE descriptions**. None are null.
- New enum `SpecificationAccuracy` (`Verified` / `Illustrative`), stored per value:
  `SeatsAccuracy` and `MaxRoadSpeedAccuracy` on `VehicleCatalogItems`, copied to `Vehicles` on creation.
- `Illustrative` = plausible SkyShift sample data, **not** a manufacturer specification. `Verified` = official
  manufacturer document (see `CATALOG-MANIFEST.md`). Verified today: seats for items 6, 19, 21; speed for 4 and 21.
- Identification (`Identified` / `VisualInspiration`) stays separate. `VisualInspiration` items may only carry
  illustrative values (domain validation).
- Domain validation: seats 1–100 and speed 1–600 km/h are required, both descriptions are required (≤ 500 chars),
  and both accuracy values must be defined.
- **New migration** `20261011090000_CompleteCatalogSpecifications`. The earlier `20261010203000_AddVehicleCatalog`
  was **not modified**, because it may already be applied. The new migration adds two nullable columns to `Vehicles`
  (left NULL), adds the two accuracy columns to the catalog, updates **only** the 22 catalog rows, then makes seats,
  speed and both accuracy columns NOT NULL in the catalog. It does not update or delete any `Vehicles` row.
- Fleet vehicles already created from the catalog keep their stored values. Their accuracy fields stay `null`.
  The manual refresh option is in `CATALOG-MANIFEST.md`.
- Descriptions now include "Specifications marked as illustrative are SkyShift sample data, not manufacturer figures."
  whenever an item has an illustrative value, in both languages.

## 1. What already existed (kept, not re-implemented)

- **Gender:** `UserGender` (Male=1, Female=2), nullable `Users.Gender` stored as a string, enum validation on
  registration, returned by register/login as `"Male"`/`"Female"`/`null`. Legacy users stay `null`.
- **Flight validation:** UTC-only (`Z`), inclusive window `StartDate ≤ date ≤ EndDate`, not after server
  "now", user and vehicle taken from the token and the active assignment, unique index `(UserId, Date)`,
  duplicates → 409 `duplicate_flight`, and the existing migration guard that stops (without deleting)
  if old duplicates exist.
- **Double vehicle assignment protection** (RowVersion + Serializable transaction): untouched.

## 2. What was added

- **Catalog** (`VehicleCatalogItems` table, 22 seeded models, explicit ids 1–22 ↔ `/catalog/car-NN.webp`).
  Fields: manufacturer, model, seats (nullable), **max road speed** (nullable), short EN/HE descriptions,
  relative image path, identification (`Identified` / `VisualInspiration`). See `CATALOG-MANIFEST.md`.
- **Catalog-based creation:** `POST /api/manager/vehicles` now accepts only `catalogItemId` and
  `isPersonalExperimental` (the personal-experimental option is kept). All metadata is copied from the
  catalog on the server. The vehicle Id is still the database identity.
- **No duplicates, including concurrent ones:** application check, plus a filtered unique index
  `IX_Vehicles_CatalogItemId` (`WHERE CatalogItemId IS NOT NULL`). A violation is translated to 409
  `catalog_item_in_use`. A used model never returns to the pool, even if its vehicle is retired.
- **New manager endpoint:** `GET /api/manager/vehicles/catalog` (unused models only).
- **Vehicle responses** (user and manager) now include `catalogItemId`, `manufacturer`, `description`,
  `descriptionHe`, `catalogIdentification`, `maxRoadSpeedKmh` (catalog-linked vehicles only).
  `maxSpeedKmh`, `seats`, `imageUrl`, `modelName` are unchanged in name.
- **Error codes** in the same `code`/`messageHe`/`messageEn` contract: `duplicate_email` (409, same English
  text as before), `invalid_credentials` (401, now with a body), `catalog_item_not_found` (404),
  `catalog_item_in_use` (409). A concurrent duplicate registration is now also 409 instead of 500.
- Login no longer catches exceptions in the controller; the central handler maps them.

## 3. Changed files

Update 2:
- New: `src/SkyShift.Domain/Enums/SpecificationAccuracy.cs`,
  `src/SkyShift.Infrastructure/Migrations/20261011090000_CompleteCatalogSpecifications.cs` + `.Designer.cs`
- Modified: `Domain/Entities/VehicleCatalogItem.cs`, `Domain/Entities/Vehicle.cs`,
  `Application/DTOs/VehicleCatalogResponse.cs`, `VehicleResponse.cs`, `ManagerVehicleResponse.cs`,
  `Infrastructure/Persistence/Configurations/VehicleCatalogItemConfiguration.cs`, `VehicleConfiguration.cs`,
  `Infrastructure/Migrations/SkyShiftDbContextModelSnapshot.cs`,
  tests `Domain/CatalogItemTests.cs`, `UseCases/CatalogUseCaseTests.cs`, `Persistence/CatalogModelTests.cs`,
  `Concurrency/CatalogSqlServerTests.cs`, and `CATALOG-MANIFEST.md`, `CATALOG-UPDATE.md`.
- Not modified: `20261010203000_AddVehicleCatalog.cs` / `.Designer.cs` and all earlier migrations.

First update:

New:
- `src/SkyShift.Domain/Entities/VehicleCatalogItem.cs`
- `src/SkyShift.Domain/Enums/CatalogIdentification.cs`
- `src/SkyShift.Domain/Exceptions/CatalogItemInUseException.cs`, `CatalogItemNotFoundException.cs`,
  `EmailAlreadyRegisteredException.cs`, `InvalidCredentialsException.cs`
- `src/SkyShift.Application/Abstractions/IVehicleCatalogRepository.cs`
- `src/SkyShift.Application/DTOs/VehicleCatalogResponse.cs`
- `src/SkyShift.Application/UseCases/GetVehicleCatalog/GetVehicleCatalogUseCase.cs`
- `src/SkyShift.Infrastructure/Persistence/Configurations/VehicleCatalogItemConfiguration.cs`
- `src/SkyShift.Infrastructure/Repositories/VehicleCatalogRepository.cs`
- `src/SkyShift.Infrastructure/Migrations/20261010203000_AddVehicleCatalog.cs` + `.Designer.cs`
- `tests/SkyShift.Tests/Api/ErrorContractAndAuthorizationTests.cs`
- `tests/SkyShift.Tests/Concurrency/CatalogSqlServerTests.cs`
- `tests/SkyShift.Tests/Domain/CatalogItemTests.cs`
- `tests/SkyShift.Tests/Persistence/CatalogModelTests.cs`
- `tests/SkyShift.Tests/UseCases/CatalogUseCaseTests.cs`
- `CATALOG-MANIFEST.md`, `CATALOG-UPDATE.md`

Modified:
- `src/SkyShift.Domain/Entities/Vehicle.cs` (catalog fields, `ApplyCatalogItem`, edits locked when linked)
- `src/SkyShift.Application/DTOs/CreateVehicleRequest.cs`, `VehicleResponse.cs`, `ManagerVehicleResponse.cs`
- `src/SkyShift.Application/Mapping/MappingProfile.cs`
- `src/SkyShift.Application/UseCases/CreateVehicle/CreateVehicleUseCase.cs`, `LoginUser/LoginUserUseCase.cs`,
  `RegisterUser/RegisterUserUseCase.cs`
- `src/SkyShift.Infrastructure/DependencyInjection.cs`, `Persistence/SkyShiftDbContext.cs`,
  `Persistence/UnitOfWork.cs`, `Persistence/Configurations/VehicleConfiguration.cs`,
  `Migrations/SkyShiftDbContextModelSnapshot.cs`
- `src/SkyShift.Api/Program.cs`, `Controllers/ManagerVehiclesController.cs`, `Controllers/AuthController.cs`,
  `Middleware/SkyShiftExceptionHandler.cs`
- `tests/SkyShift.Tests/SkyShift.Tests.csproj` (project reference to SkyShift.Api; no new packages),
  `UseCases/RegistrationAndVehicleMetadataTests.cs` (old free-form create test replaced), `UseCases/RecordFlightUseCaseTests.cs`

No package versions changed. No existing migration was edited.

## 4. Verification — what actually ran

The work environment could not reach NuGet (blocked by network policy), so the solution could **not** be
restored, fully built or tested here. What did run, with the .NET SDK 8.0.131 from Ubuntu:

First update, run by you on your machine: real build passed; 58 tests, 57 passed, 1 failed
(`CatalogModelTests` read seed data from the runtime model). That test was fixed to use `IDesignTimeModel`.

Update 2, run here (NuGet still unreachable):

| Check | Result |
|---|---|
| All 22 seed rows passed through `VehicleCatalogItem.Create` validation and copied to a `Vehicle` | **Passed** (22/22) |
| `SkyShift.Domain` full build, `-warnaserror` | **Passed** (0 warnings, 0 errors) |
| `SkyShift.Application` build against a local stand-in with AutoMapper 15's signatures (not the real package) | **Passed** (0 warnings, 0 errors). Type-level check only. |
| Roslyn syntax parse of all 121 C# files (src + tests), C# 12 | **Passed** (0 diagnostics) |
| Infrastructure / Api / Tests compilation | **Not run** (needs EF Core, ASP.NET packages, xUnit, Moq) |
| `dotnet test` (existing and new tests) | **Not run** |
| SQL Server tests (concurrency, migration preservation) | **Not run** (no SQL Server) |
| EF snapshot/model consistency (`HasPendingModelChanges`) | **Not run** for update 2. The new snapshot and Designer were written by hand, so this is the first thing to check locally. |
| Generated migration SQL | **Not generated**. The expected statements are listed below. |

## 5. Run locally (in the folder with `SkyShift.sln`)

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
# Optional, isolated SQL Server tests (temporary SkyShiftTests_* databases, never SkyShiftDb):
$env:SKYSHIFT_TEST_SQLSERVER = 'Server=(localdb)\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True'
dotnet test --no-build --filter "Category=SqlServer"
Remove-Item Env:SKYSHIFT_TEST_SQLSERVER
```

The model check is in `CatalogModelTests.CatalogMigration_OnlyAddsSchemaAndSeed_NeverRewritesExistingVehicles`.
If it reports pending model changes, **do not apply the migrations**. Instead, delete the two
`20261011090000_CompleteCatalogSpecifications*` files, restore the snapshot to the version from the first update, and run
`dotnet ef migrations add CompleteCatalogSpecifications --project src/SkyShift.Infrastructure --startup-project src/SkyShift.Api`.
Then check that the generated `Up` fills the catalog rows **before** the `AlterColumn ... nullable: false` calls
(reorder by hand if needed), and compare it with the hand-written version before applying.

Inspect the SQL before applying:

```powershell
dotnet tool restore
dotnet ef migrations script 20261006230426_AddFlightGuardsGenderAndVehicleDetails 20261010203000_AddVehicleCatalog --project src/SkyShift.Infrastructure --startup-project src/SkyShift.Api -o catalog-migration.sql
```

```powershell
dotnet ef migrations script 20261010203000_AddVehicleCatalog 20261011090000_CompleteCatalogSpecifications --project src/SkyShift.Infrastructure --startup-project src/SkyShift.Api -o catalog-specs-migration.sql
```

Expected content of the second script: two `ALTER TABLE [Vehicles] ADD ... nvarchar(20) NULL`, two
`ALTER TABLE [VehicleCatalogItems] ADD ...`, 22 `UPDATE [VehicleCatalogItems]`, then `ALTER COLUMN ... NOT NULL` for
`Seats`, `MaxRoadSpeedKmh`, `SeatsAccuracy`, `MaxRoadSpeedAccuracy`. EF's default-constraint lookup
(`DROP CONSTRAINT` inside a `DECLARE` block) is normal. There should be **no** `UPDATE [Vehicles]`, `DELETE FROM` or `DROP COLUMN`.

Expected content of the first script: 5 `ALTER TABLE [Vehicles] ADD` of nullable columns, `CREATE TABLE [VehicleCatalogItems]`,
22 `INSERT INTO [VehicleCatalogItems]`, `CREATE UNIQUE INDEX [IX_Vehicles_CatalogItemId] ... WHERE [CatalogItemId] IS NOT NULL`,
`CREATE UNIQUE INDEX [IX_VehicleCatalogItems_ImageUrl]`, one foreign key (`ON DELETE NO ACTION`), and the history row.
There should be **no** `UPDATE [Vehicles]`, `DELETE FROM` or `DROP`.

## 6. Backup and apply (only after your approval)

1. Stop the API.
2. Back up the database, for example in SSMS: right-click `SkyShiftDb` → Tasks → Back Up…, or
   `BACKUP DATABASE [SkyShiftDb] TO DISK = N'C:\Backups\SkyShiftDb-before-catalog.bak' WITH INIT, CHECKSUM;`
   then `RESTORE VERIFYONLY FROM DISK = N'C:\Backups\SkyShiftDb-before-catalog.bak';`
3. Apply: `dotnet ef database update --project src/SkyShift.Infrastructure --startup-project src/SkyShift.Api`
4. Check: `SELECT COUNT(*) FROM VehicleCatalogItems WHERE Seats IS NOT NULL AND MaxRoadSpeedKmh IS NOT NULL;`
   returns 22, and existing vehicle rows are unchanged.

`dotnet ef database update` applies every pending migration in order: `AddFlightGuardsGenderAndVehicleDetails`
(if not yet applied), `AddVehicleCatalog` (if not yet applied), then `CompleteCatalogSpecifications`.

The previous migration `20261006230426_AddFlightGuardsGenderAndVehicleDetails` applies first if it hasn't
been applied yet. It stops safely if duplicate flight reports exist.

## 7. Readiness

- **Code:** written and statically checked as described in section 4; not yet compiled end-to-end or tested.
- **Database migration:** prepared, not applied, not yet validated against the model.
- **Deployment:** not ready until section 5 passes locally and the frontend changes in section 8 are made.

## 8. Frontend integration notes

### Endpoints

| Method | Path | Auth | Body / result |
|---|---|---|---|
| GET | `/api/manager/vehicles/catalog` | Manager | `VehicleCatalogResponse[]` (unused models only) |
| POST | `/api/manager/vehicles` | Manager | `{ "catalogItemId": 4, "isPersonalExperimental": false }` → 201 `ManagerVehicleResponse` |
| GET | `/api/manager/vehicles` | Manager | `ManagerVehicleResponse[]` (now with catalog fields) |
| GET | `/api/vehicles/available`, `/api/vehicles/mine`, `/api/dashboard` | User | `VehicleResponse` now includes catalog fields |
| POST | `/api/auth/login` | — | 401 now has a ProblemDetails body with `code: "invalid_credentials"` |

Catalog item (every field is always present; `seats` and `maxRoadSpeedKmh` are never null):

```json
{
  "id": 4,
  "manufacturer": "Bugatti",
  "modelName": "Chiron Super Sport",
  "seats": 2,
  "seatsAccuracy": "Illustrative",
  "maxRoadSpeedKmh": 440,
  "maxSpeedKmh": 440,
  "maxRoadSpeedAccuracy": "Verified",
  "description": "Hypercar based on the Bugatti Chiron Super Sport. The 440 km/h road top speed is the manufacturer's electronically limited figure. Specifications marked as illustrative are SkyShift sample data, not manufacturer figures. The propellers are a SkyShift illustration; the road car does not fly.",
  "descriptionHe": "מכונית־על המבוססת על Bugatti Chiron Super Sport. מהירות הכביש המרבית של 440 קמ״ש היא הנתון של היצרן, מוגבלת אלקטרונית. מפרטים המסומנים כהמחשה הם נתוני דוגמה של SkyShift ואינם נתוני יצרן. המדחפים הם הדמיה של SkyShift בלבד; רכב הכביש אינו טס.",
  "imageUrl": "/catalog/car-04.webp",
  "identification": "Identified"
}
```

Created vehicle (manager):

```json
{
  "catalogItemId": 4, "manufacturer": "Bugatti", "modelName": "Chiron Super Sport",
  "description": "…", "descriptionHe": "…", "catalogIdentification": "Identified",
  "maxSpeedKmh": 440, "maxRoadSpeedKmh": 440, "maxRoadSpeedAccuracy": "Verified",
  "seats": 2, "seatsAccuracy": "Illustrative", "imageUrl": "/catalog/car-04.webp",
  "id": 73, "status": "Available", "isPersonalExperimental": false, "createdAt": "2026-10-11T09:30:00Z"
}
```

Vehicle responses: `seatsAccuracy` / `maxRoadSpeedAccuracy` are `"Verified"`, `"Illustrative"` or `null`.
`null` means a legacy vehicle, or a vehicle created from the catalog before update 2. Its numbers are unclassified,
so show them without either label (or "—" if empty).

### Required UI labels for specification accuracy

Show a label next to **each** value whose accuracy is `Illustrative` (seats and/or speed), plus a one-line
explanation on the card or form. Verified values get the verified label. Identification labels stay separate.

| Purpose | English | Hebrew |
|---|---|---|
| Badge for an illustrative value | Illustrative | להמחשה |
| Explanation (tooltip / footnote) | Illustrative SkyShift data — not a manufacturer specification | נתוני המחשה של SkyShift — אינם מפרט יצרן |
| Badge for a verified value | Manufacturer specification | מפרט יצרן |
| Speed field label | Maximum road speed (km/h) | מהירות כביש מרבית (קמ״ש) |
| Seats field label | Seats | מושבים |
| Identification: `VisualInspiration` | Visual inspiration — model not verified | השראה חזותית — הדגם לא אומת |
| Always on catalog vehicles | Propellers are a SkyShift illustration; the road car does not fly | המדחפים הם הדמיה של SkyShift; רכב הכביש אינו טס |

User `VehicleResponse` has the same catalog fields. `status` stays numeric (0–4) as before.

Error body example (`Accept-Language: he` puts the Hebrew text in `detail`):

```json
{ "status": 409, "title": "Catalog model already used", "detail": "This catalog model has already been added to the fleet. Choose another model.",
  "code": "catalog_item_in_use", "messageHe": "הדגם הזה כבר נוסף לצי הרכבים. יש לבחור דגם אחר.", "messageEn": "This catalog model has already been added to the fleet. Choose another model." }
```

| code | HTTP |
|---|---|
| `duplicate_email` | 409 |
| `invalid_credentials` | 401 |
| `catalog_item_not_found` | 404 |
| `catalog_item_in_use` | 409 |
| `invalid_vehicle_catalog` | 400 |
| `flight_time_zone_required`, `flight_outside_trial`, `flight_in_future` | 400 |
| `duplicate_flight` | 409 |

A missing or zero `catalogItemId` returns the standard 400 validation response (`errors.CatalogItemId`).

### Required frontend changes (not made)

1. **Bug — the catalog never loads:** in `src/api.ts`, `api.catalog` calls
   `request("/api/manager/vehicles/catalog")`, but `request()` already prefixes `/api`, so the real URL
   becomes `/api/api/manager/vehicles/catalog` (404). Change it to `request<CatalogItem[]>("/manager/vehicles/catalog", { signal })`.
   The Playwright mock `**/api/manager/vehicles/catalog` also matches the wrong URL, which hid this.
2. **Wording:** `components.tsx` shows "Visual inspiration · simulated flight specifications" (HE: "מפרט טיסה לסימולציה")
   for every catalog vehicle. Replace it with the labels above: per-value `Illustrative` / `Manufacturer specification`
   badges driven by `seatsAccuracy` / `maxRoadSpeedAccuracy`, the propeller sentence, and the visual-inspiration line only
   when `catalogIdentification === "VisualInspiration"`. In `manager-pages.tsx`, rename "Maximum speed (km/h)" to
   "Maximum road speed (km/h)" and replace "Current specifications are illustrative" with badges per field.
   Add the Hebrew strings to `he.json`.
3. **Types:** `CatalogItem` keeps `seats: number` and `maxSpeedKmh: number` (correct now: never null). Add
   `maxRoadSpeedKmh: number`, `seatsAccuracy` and `maxRoadSpeedAccuracy` (`"Verified" | "Illustrative"`),
   `manufacturer: string`, `identification`. On `VehicleMetadata` add the same accuracy fields as optional/nullable.
4. **Optional:** show `manufacturer` next to `modelName` and use `maxRoadSpeedKmh` explicitly. Existing code keeps
   working through `maxSpeedKmh`.
5. Images: all 22 seeded paths already exist in `public/catalog`. No asset is missing for seeded items.

## 9. Unresolved / for your decision

- Most catalog figures are **Illustrative SkyShift data** (19 seat values, 20 speed values), because official pages
  could not be read here. They are flagged per value; the 5 verified values and their sources are in `CATALOG-MANIFEST.md`.
- Vehicles already created from the catalog before update 2 keep their old (possibly empty) seats/speed with no accuracy;
  a reviewed manual refresh is described in `CATALOG-MANIFEST.md`.
- Four items are flagged `VisualInspiration` (2, 7, 13, 17). Please check them visually.
- Breaking change: old clients that send free-form `modelName`/`maxSpeedKmh`/`seats`/`imageUrl` without
  `catalogItemId` now get 400. Those extra fields are ignored if a valid `catalogItemId` is sent.
- `PROJECT_DECISIONS.md` describes an earlier layer-approval process. This update is documented here and was
  not added to that log.
- Flight reports remain self-reported. Timestamp and duplicate checks do not prove that a flight took place.
