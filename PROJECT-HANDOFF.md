# SkyShift — Project Handoff

_Last updated: 2026-10-12 (Asia/Jerusalem). Based on the actual files and Git status at that time. This file contains no passwords, tokens or connection strings._

## 1. Current state

| Item | State |
|---|---|
| Backend repo `C:\Users\9090\Desktop\פרויקט` (`rl6765936-creator/SkyShift`, `main`) | HEAD `747cf7e` (pushed). **60 files changed, all uncommitted**: PostgreSQL support, dependency security fixes, XML summary cleanup, README, this file. |
| Frontend repo `C:\Users\9090\Desktop\פרויקט ריאקט\skyshift-react` (`rl6765936-creator/SkyShift-React`, `main`) | HEAD `33b2150` (pushed), clean. It already contains the video autoplay fallback (muted + "Enable sound") and the IsPersonalExperimental removal. **No frontend code change is needed**; only a Render environment variable. |
| Live frontend | `https://skyshift-react.onrender.com`. It cannot reach an API yet. |
| API on Render | Not created yet. |
| Neon (project `blue-cloud-29061764`, branch `production`) | **Not touched.** Its contents have not been inspected (that needs a private connection; see §7, step 2). |
| Local SQL Server (LocalDB `SkyShiftDb`) | Not touched. |

## 2. Agreed requirements and decisions

- **Platform and architecture:** .NET 8, Clean Architecture (Domain / Application / Infrastructure / Api), unchanged API contracts and bilingual errors, and the 22-item catalog.
- **Database providers:** SQL Server is the default. PostgreSQL is selected with `Database:Provider=PostgreSQL` and has its own migrations project, `SkyShift.Infrastructure.PostgreSql`.
- **Hosting:** Render Docker web service in Frankfurt, plus Neon PostgreSQL in Frankfurt (`aws-eu-central-1`). Free plans only.
- **Public demo accounts:** the User and Manager accounts with their documented passwords are intentional. `SeedDemoUsersUseCase` runs on every startup in every environment, in Production too, and needs no flag. It is idempotent: an account is created only if its email is missing. In a rehearsal, starting the API twice left exactly 2 users.
- **Authentication:** JWT, kept as is (no Neon Auth).
- **Secrets:** they live only in User Secrets (local) or in Render environment variables. Never in Git, the browser bundle or logs.
- **No retries:** no `EnableRetryOnFailure`, no broad error mapping, and no weakened tests.
- **PostgreSQL locking:** Read Committed with `FOR UPDATE` on the vehicle row and `FOR NO KEY UPDATE` on the user row. SQL Server keeps Serializable with `UPDLOCK, HOLDLOCK`.
- **Neon connection:** use the **direct** (non-pooled) endpoint for both migrations and the app, with `Maximum Pool Size=10;Connection Idle Lifetime=60`.
  - The direct endpoint avoids PgBouncer transaction-pooling subtleties.
  - The idle lifetime drops pooled connections before Neon's free compute suspends, so the first request after an idle period does not hit a dead connection.

## 3. Changes in this round (uncommitted)

### 3a. Dependency security fixes

The fix updates the parent packages; no vulnerable package is pinned on its own.

| Project | Package | Before | After |
|---|---|---|---|
| Infrastructure | Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | **8.0.31** |
| Infrastructure | Microsoft.EntityFrameworkCore.Design (PrivateAssets) | 8.0.0 | **8.0.31** |
| Infrastructure | Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.0 | **8.0.11** (latest provider for EF Core 8; requires EF ≥ 8.0.11 and Npgsql ≥ 8.0.6) |
| Infrastructure | Npgsql (direct pin) | 8.0.6 | **removed**; 8.0.6 now comes from the provider |
| Api | Microsoft.EntityFrameworkCore.Design | 8.0.0 | **8.0.31** |
| Tests | Microsoft.EntityFrameworkCore.InMemory | 8.0.0 | **8.0.31** |

8.0.31 (released 2026-09-08) is the newest EF Core 8 servicing release on nuget.org. It also matches the `Microsoft.AspNetCore.Authentication.JwtBearer 8.0.31` the API already uses.

**Vulnerable transitive packages before the change**

These were found in the previous restore graph (`obj/project.assets.json`) and checked against the official advisories.

| Package | Resolved | Advisory | Fixed in | Introduced by |
|---|---|---|---|---|
| Microsoft.Data.SqlClient | 5.1.1 | CVE-2024-0056 (high) | 5.1.3 | EF Core SqlServer 8.0.0 |
| Azure.Identity | 1.7.0 | CVE-2023-36414, CVE-2024-29992, CVE-2024-35255 | 1.10.2 / 1.11.0 / 1.11.4 | SqlClient 5.1.1 |
| System.IdentityModel.Tokens.Jwt | 6.24.0 (Infrastructure, PostgreSQL migrations project) | CVE-2024-21319 | 6.34.0 | SqlClient 5.1.1 |
| System.Formats.Asn1 | 5.0.0 | CVE-2024-38095 (high) | 6.0.1 / 8.0.1 | SqlClient 5.1.1 |
| System.Text.Json | 8.0.0 (Infrastructure) | CVE-2024-43485 | 8.0.5 | EF Design 8.0.0 |
| Microsoft.Extensions.Caching.Memory | 8.0.0 | CVE-2024-43483 | 8.0.1 | EF Core 8.0.0 |

**After the change**, per the official nuget.org metadata:
- EF SqlServer 8.0.31 depends on Microsoft.Data.SqlClient ≥ 5.1.9 and System.Formats.Asn1 ≥ 8.0.2.
- SqlClient 5.1.9 depends on Azure.Identity ≥ 1.12.1, Microsoft.Identity.Client ≥ 4.76.0 and Microsoft.IdentityModel.* ≥ 6.35.0.
- EF Core 8.0.x patches use Caching.Memory ≥ 8.0.1.

### 3b. Documentation

- **XML summaries:** 48 summaries were shortened to at most two English sentences, so now every summary in `src`, `tests` and `tools` has at most two. This is documentation only: with the `///` lines removed, the files are identical to before.
- **README:**
  - The "PostgreSQL / Neon" section was rewritten as a short checklist that points to §7.
  - A new "Frontend (Render Static Site)" section was added.

### 3c. Earlier in this branch (also uncommitted)

- **PostgreSQL support:** provider selection, `PostgreSqlModel`, locking, error codes, the migrations project `SkyShift.Infrastructure.PostgreSql` (migration `20261011044237_InitialPostgreSql`), and the tests `PostgreSqlTests` (13) and `ProviderModelTests` (9).
- **Wiring:** the solution entry, the Dockerfile `COPY` line and the README sections.

## 4. Verification: what actually ran

The verification ran in a cloud session. It had **no access to nuget.org and no Docker daemon**, but it did have the **official .nupkg files from your NuGet cache** and a real PostgreSQL 16.15 server.

| Check | Packages used | Result |
|---|---|---|
| `dotnet restore` of the solution | official nupkgs from your cache: EF Core **8.0.28**, SqlClient 5.1.7, EFCore.PG 8.0.0, Npgsql 8.0.6, InMemory 8.0.0 (the closest versions in your cache to the final ones) | succeeded |
| `dotnet build` of the solution | same | 0 errors |
| Full test suite with real PostgreSQL 16 | same | **135 passed, 0 failed, 11 skipped** (the 11 SQL Server tests, no SQL Server there) |
| Full test suite with EFCore.PG **8.0.11** built from its official tag (v8.0.11) | EF 8.0.28 | 135 passed, 0 failed, 11 skipped |
| PostgreSQL tests (category `PostgreSql`) | — | 13/13 passed. They cover: concurrent assignment vs deletion (both winners), concurrent assignments, same user selecting twice, duplicate registrations (any letter case), duplicate catalog creation, duplicate flights, UTC dates, rollback consistency, the `xmin` token, migrations and the 22 catalog items |
| `dotnet ef migrations has-pending-model-changes`, SQL Server project | real dotnet-ef 8.0.0 | "No changes have been made to the model since the last migration." |
| Same command, PostgreSQL project | real dotnet-ef 8.0.0 | same result |
| `dotnet ef migrations script --idempotent` (PostgreSQL), run with a placeholder connection | — | 220 lines; only CREATE statements plus 22 INSERTs; no DROP, DELETE or TRUNCATE |
| `dotnet ef database update` against an empty local PostgreSQL ("Neon rehearsal") | — | migration applied; 22 catalog rows, each complete and with `/catalog/car-NN.webp`; 0 users |
| Published API in Production on that database, started twice | — | started both times; demo users = 2 after both starts; CORS allows only `https://skyshift-react.onrender.com`; no ERROR lines and no secrets in the logs |
| Vulnerability check of the restored graph against the advisories in §3a | 8.0.28 graph | no package in any vulnerable range |

### Not yet verified (pending on your machine or on Render)

1. **Restore and build with the exact final versions** (EF 8.0.31, SqlClient 5.1.9, EFCore.PG 8.0.11 from nuget.org), and `dotnet list package --vulnerable --include-transitive` against the live NuGet vulnerability database.
2. **The 11 SQL Server tests on the new package versions.** Your earlier SQL Server pass used EF 8.0.0.
3. **A real `docker build` and container start.** No Docker daemon was available; the first real build will be on Render.
4. **Anything against Neon or Render.**

## 5. Local verification commands (PowerShell, in `C:\Users\9090\Desktop\פרויקט`)

```powershell
dotnet restore
dotnet build --no-restore
dotnet list package --vulnerable --include-transitive   # expected: no vulnerable packages in any project
dotnet list package --outdated                           # optional, informational

# Tests without a database (SQL Server and PostgreSQL tests are skipped):
dotnet test --no-build

# SQL Server tests (temporary SkyShiftTests_* databases; SkyShiftDb is never touched):
$env:SKYSHIFT_TEST_SQLSERVER = "Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True"
dotnet test --no-build --filter "Category=SqlServer"

# PostgreSQL tests: a disposable TEST server only, never Neon. For example, with Docker Desktop:
#   docker run --name skyshift-pg-test -e POSTGRES_PASSWORD=<local-test-password> -p 5433:5432 -d postgres:16
$env:SKYSHIFT_TEST_POSTGRES = Read-Host "Test PostgreSQL connection (Host=localhost;Port=5433;Username=postgres;Password=...)"
dotnet test --no-build --filter "Category=PostgreSql"

# Everything (both variables still set):
dotnet test --no-build
Remove-Item Env:SKYSHIFT_TEST_SQLSERVER, Env:SKYSHIFT_TEST_POSTGRES

# Optional, only if Docker Desktop is installed:
docker build -t skyshift-api .
```

Expected results:
- with both servers: 146 tests, 0 failed, 0 skipped;
- with SQL Server only: 133 passed, 13 skipped (the PostgreSQL tests).

The warning "EF tools version 8.0.0 is older than the runtime" is harmless.

## 6. Deployment configuration review

- **Dockerfile:**
  - .NET 8 SDK → ASP.NET 8 runtime.
  - Restore runs after copying all five `.csproj` files, including the PostgreSQL migrations project.
  - Production, non-root user, `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` (Render terminates HTTPS and forwards the scheme).
- **`.dockerignore`:** excludes `bin/`, `obj/`, `.git`, tests, tools, logs, `.env*`, keys and backups.
- **Port:** the API listens on `PORT` when set (`Hosting/PlatformPort.cs`, unit-tested).
- **Logging:** NLog writes to the console, which Render shows. The file target cannot write as a non-root user and fails silently.
- **CORS:** only `https://skyshift-react.onrender.com`. All headers are allowed, which includes `Authorization` and `Accept-Language`.
- **Frontend API helper:**
  - `src/api.ts` uses `${VITE_API_BASE_URL}/api${path}` and strips one trailing slash.
  - So set `VITE_API_BASE_URL=https://<api-service>.onrender.com`, with no `/api` and no trailing slash.
  - Catalog images are frontend assets (`public/catalog/car-01…22.webp`) referenced by the API as `/catalog/car-NN.webp`, so they load from the frontend origin.
- **SPA rewrite:** the Static Site needs Redirects/Rewrites: Source `/*` → Destination `/index.html`, Action **Rewrite**. I could not reach the live site from here to confirm it is set; check it in the Render dashboard.

## 7. Neon procedure: prepared, not run

Do these steps in order. Stop at any check that fails.

### 1. Find the direct connection details
In the Neon Console (project `blue-cloud-29061764`, branch `production`), open **Connect**:
- Turn **Connection pooling OFF** to get the direct host: `ep-....eu-central-1.aws.neon.tech`, with **no** `-pooler`.
- Note the database (usually `neondb`) and the role.

### 2. Inspect the database read-only
In the Neon **SQL Editor**, on branch `production`, run:
```sql
SELECT table_schema, table_name FROM information_schema.tables
WHERE table_schema NOT IN ('pg_catalog','information_schema') ORDER BY 1, 2;
SELECT collname FROM pg_collation WHERE collname = 'skyshift_case_insensitive';
```
Continue **only if** none of these exist: `Users`, `Vehicles`, `VehicleCatalogItems`, `VehicleAssignments`, `Flights`, `__EFMigrationsHistory`, or the collation.

If any of them exists, **stop**: do not drop, reset or overwrite anything, and report what you see.

### 3. Create a backup branch first (free)
Neon Console → Branches → **Create branch** from `production`, named `pre-skyshift-schema`. This is the rollback point.

### 4. Generate and review the SQL (no real connection is used)
```powershell
cd "C:\Users\9090\Desktop\פרויקט"
dotnet tool restore
$env:Database__Provider = "PostgreSQL"
$env:ConnectionStrings__SkyShiftDb = "Host=script-only.invalid;Database=none"   # placeholder; never contacted
dotnet ef migrations script --idempotent --project src/SkyShift.Infrastructure.PostgreSql --startup-project src/SkyShift.Api -o "$env:TEMP\neon-schema.sql"
Select-String -Path "$env:TEMP\neon-schema.sql" -Pattern 'DROP|DELETE|TRUNCATE'   # must print nothing
Select-String -Path "$env:TEMP\neon-schema.sql" -Pattern 'CREATE (TABLE|COLLATION|UNIQUE INDEX|INDEX)'
```
The script creates only the following:
- the collation `skyshift_case_insensitive` (ICU, case-insensitive email);
- the tables `Users`, `VehicleCatalogItems`, `Vehicles`, `Flights`, `VehicleAssignments`, plus `__EFMigrationsHistory`;
- 4 unique indexes (`IX_Users_Email`, `IX_Flights_UserId_Date`, `IX_Vehicles_CatalogItemId` filtered on non-null, `IX_VehicleCatalogItems_ImageUrl`) and 3 plain indexes;
- 22 `INSERT`s into `VehicleCatalogItems`.

It never uses the SQL Server migrations. The file is written to `%TEMP%`, outside the repo.

### 5. Enter the real connection privately (masked) and validate it
```powershell
$sec = Read-Host "Neon DIRECT connection string (key=value format)" -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec)
$env:ConnectionStrings__SkyShiftDb = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr); Remove-Variable sec, bstr
$cs = $env:ConnectionStrings__SkyShiftDb
if ($cs -match '^\s*postgres(ql)?://')                                { throw "Use Host=...;Database=... format, not a URL" }
if ($cs -notmatch '(?i)Host=ep-[^;]+\.eu-central-1\.aws\.neon\.tech') { throw "Not a Frankfurt Neon host" }
if ($cs -match '(?i)-pooler\.')                                        { throw "Use the direct (non-pooled) host" }
if ($cs -notmatch '(?i)SSL ?Mode=Require')                             { throw "Add SSL Mode=Require" }
Remove-Variable cs
$env:Database__Provider = "PostgreSQL"
```
The format is `Host=ep-....eu-central-1.aws.neon.tech;Database=neondb;Username=<role>;Password=<password>;SSL Mode=Require`.

### 6. Read-only check through EF
```powershell
dotnet ef migrations list --project src/SkyShift.Infrastructure.PostgreSql --startup-project src/SkyShift.Api
```
Expected: exactly `20261011044237_InitialPostgreSql (Pending)`. Anything else means **stop**.

### 7. Apply — this is the first and only step that modifies Neon
```powershell
dotnet ef database update --project src/SkyShift.Infrastructure.PostgreSql --startup-project src/SkyShift.Api
Remove-Item Env:Database__Provider, Env:ConnectionStrings__SkyShiftDb
```
PostgreSQL runs the migration in one transaction, so if it fails nothing is left half-created. `dotnet ef` stops before the API starts, so no users are seeded in this step.

### 8. Verify in the Neon SQL Editor
```sql
SELECT "MigrationId" FROM "__EFMigrationsHistory";                         -- 20261011044237_InitialPostgreSql
SELECT count(*) FROM "VehicleCatalogItems";                                  -- 22
SELECT count(*) FROM "VehicleCatalogItems"
 WHERE "ImageUrl" = '/catalog/car-' || lpad("Id"::text, 2, '0') || '.webp'
   AND "Seats" > 0 AND "MaxRoadSpeedKmh" > 0
   AND length("Description") > 0 AND length("DescriptionHe") > 0;           -- 22
SELECT count(*) FROM "Users";                                                 -- 0 until the API first starts
```

## 8. Render Web Service (free plan, Frankfurt)

**Create:** New → Web Service, repo `rl6765936-creator/SkyShift`, branch `main`.
- Region **Frankfurt (EU Central)**.
- Runtime **Docker**: Dockerfile path `./Dockerfile`, Docker build context `.`.
- Instance type **Free**.
- Health check path: leave empty. Auto-deploy: on.

**Public settings** (plain environment variables):
- `Database__Provider` = `PostgreSQL`
- `Jwt__Issuer` = `SkyShift.Api`
- `Jwt__Audience` = `SkyShift.Client`

Any stable names work for issuer and audience, as long as they never change after tokens have been issued.

**Secrets** (environment variables; never in Git):
- `ConnectionStrings__SkyShiftDb` — the Neon **direct** string with `;Maximum Pool Size=10;Connection Idle Lifetime=60` appended.
- `Jwt__SigningKey` — at least 32 random bytes. Generate it in Windows PowerShell and paste it straight into Render:
  ```powershell
  $b = New-Object byte[] 48; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); [Convert]::ToBase64String($b) | Set-Clipboard; Remove-Variable b
  ```

**Do not set:**
- `PORT` (Render provides it);
- `ASPNETCORE_ENVIRONMENT` (the image already runs Production; do not switch to Development just to get Swagger).

**Demo seeding** needs no setting: it runs on every startup. Because it is idempotent, restarts are safe.

**Free-plan behaviour:** the service sleeps after about 15 minutes idle and needs a cold start of up to about a minute. Neon's free compute also suspends when idle and needs a few seconds to wake.

## 9. Frontend (Render Static Site `skyshift-react`)

1. Environment: set `VITE_API_BASE_URL` = `https://<api-service-name>.onrender.com`. No `/api`, no trailing slash. It is public, not a secret.
2. Redirects/Rewrites: make sure `/*` → `/index.html` (**Rewrite**) exists.
3. Manual Deploy → **Clear build cache & deploy**. Vite inlines the variable at build time.

## 10. Publishing (only when you decide)

### Backend
```powershell
cd "C:\Users\9090\Desktop\פרויקט"
git status --short
git add -- Dockerfile README.md SkyShift.sln PROJECT-HANDOFF.md src tests
git status --short          # tools/ and _migration-review/ must remain untracked; no bin/ or obj/
git diff --cached --stat     # expected: 60 files
git diff --cached -- . ':(exclude)PROJECT-HANDOFF.md' | Select-String -Pattern 'Password=[^<;\s]|SigningKey"\s*:\s*"[^"]+'   # must print nothing (this file is excluded only because it contains the pattern itself)
git commit -m "Add PostgreSQL (Neon) support, fix vulnerable EF Core dependencies, deployment docs"
git push origin main
```

### Frontend
No changes to commit, because `git status` is clean. Only the Render environment variable and a redeploy are needed.

### Local leftovers
These are untracked or inert; delete them if you like:
- `_migration-review\postgresql-patch\` (the applied patch and the summary edit list);
- `.git\stale-index-lock-claude-dryrun`, an empty, inert file left by a dry-run `git add -n` (along with older `index.lock.stale*` files).

## 11. Final live checks (after both deploys)

1. **Accounts and roles:**
   - Log in as the User and as the Manager.
   - Manager-only endpoints return 403 for the User and 401 without a token.
2. **Registration:**
   - A new account works.
   - The same email in different letter case returns 409 `duplicate_email`.
3. **Catalog:**
   - The Manager sees 22 complete models.
   - Images load from `/catalog/car-NN.webp`.
4. **Vehicles:**
   - Creating a vehicle returns 201.
   - Reusing the same model returns 409 `catalog_item_in_use`.
5. **Assignment, flights and deletion:**
   - User assignment: 204. A second assignment: 409.
   - A flight before the assignment start: 400. A valid flight: 204. A duplicate flight: 409.
   - Dashboards update.
   - Deleting an in-use vehicle: 409. Deleting an unused one: 204.
6. **Errors:** Hebrew and English messages follow the UI language (`Accept-Language`).
7. **Routing:** refreshing a deep route such as `/manager/vehicles` reloads the app instead of returning 404.
8. **Homepage video:** plays, falls back to muted when autoplay with sound is blocked, and "Enable sound" works.
9. **CORS:** the browser console shows no CORS errors. A request from another origin is not allowed.
10. **Unexpected errors:** a generic 500 body (no stack trace); details only in the Render logs, with a CorrelationId.

## 12. Rollback (both databases preserved)

- **API:** in Render → Deploys, **Rollback** to the previous deploy, or suspend the service.
  - In code: `git revert <commit>` and push, since the history is never rewritten.
  - SQL Server is the default provider, so removing `Database__Provider` would switch the API to SQL Server; that only makes sense with a SQL Server connection string.
- **Frontend:** in Render → Deploys, **Rollback** to the previous deploy, or clear `VITE_API_BASE_URL` and redeploy.
- **Neon:** the schema step only adds objects. To return to the exact previous state, restore from the backup branch `pre-skyshift-schema` (§7, step 3). Neon also offers point-in-time restore. Never run `dotnet ef database update 0` on a database that holds data you need.
- **Local SQL Server:** untouched by all of this. Its migrations are unchanged, and the SQL Server model has no pending changes.

## 13. Blockers and questions

**Blockers.** Nothing in the code. What remains is verification only, in this order:
1. Run §5 on Windows: official restore with the final versions, the vulnerability audit, the SQL Server tests, and optionally the PostgreSQL tests.
2. The Neon read-only inspection (§7, step 2).
3. The first Docker build, which will happen on Render.

**Questions:**
1. Do you have Docker Desktop on Windows, for an optional local `docker build` and the PostgreSQL test server? If not, the Render build is the first container build.
2. Which Render service name do you want? It decides the `VITE_API_BASE_URL` value.

## 14. Next concrete action

Run the §5 commands on Windows and report the counts and the audit output. Then follow §10 (commit and push) → §7 (Neon) → §8 (Render API) → §9 (frontend) → §11 (live checks).
