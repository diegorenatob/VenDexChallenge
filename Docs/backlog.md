# VendSys Challenge — Feature Backlog

Features are ordered for implementation. Each feature builds on the previous ones — no feature depends on anything not yet built.

---

## Feature 1 — Solution and Project Scaffolding

**Title:** Create the .NET solution and all project skeletons using `dotnet new`

**Description:**  
Initialise the solution file and all six projects in their correct folder positions. Add project references to wire up the dependency graph. Install base NuGet packages in each project. Ensure the solution builds with zero errors before any business logic is written.

**Acceptance Criteria:**
- `VendSysChallenge.sln` exists at the repo root
- Projects created: `VendSys.Domain`, `VendSys.Application`, `VendSys.Infrastructure`, `VendSys.Api`, `VendSys.Maui`, `VendSys.Tests`, `VendSys.Maui.Tests`
- Project references match the dependency graph: API → Application + Infrastructure; Infrastructure → Application + Domain; Application → Domain
- `dotnet build VendSysChallenge.sln` succeeds with zero errors and zero warnings
- All `.csproj` files target `net9.0` (or `net9.0-windows10.0.19041.0;net9.0-android;net9.0-ios` for MAUI)
- File-scoped namespaces configured as the default via `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>` in each project

**Affected Layers:** All

---

## Feature 2 — EF Core Setup: DbContext, Entity Configurations, Initial Migration

**Title:** Configure EF Core 9 with `VendSysDbContext` and create the `InitialCreate` migration

**Description:**  
Add EF Core 9 and `Microsoft.EntityFrameworkCore.SqlServer` to `VendSys.Infrastructure`. Create `VendSysDbContext` with `DbSet<DexMeter>` and `DbSet<DexLaneMeter>`. Implement `IEntityTypeConfiguration<T>` for both entities to define table names, column types, the primary keys, the unique constraint on `(Machine, DEXDateTime)`, and the FK between tables. Register the context in `VendSys.Api` using the `DefaultConnection` string. Run the first migration so the database and both tables are created in LocalDB.

**Acceptance Criteria:**
- `VendSysDbContext` registered as scoped in `Program.cs` with connection string from `appsettings.json`
- `DexMeterConfiguration` sets: table `DEXMeter`, PK `DexMeterId`, column types per database design, unique index on `(Machine, DEXDateTime)`
- `DexLaneMeterConfiguration` sets: table `DEXLaneMeter`, PK `DexLaneMeterId`, FK to `DEXMeter.DexMeterId` with cascade delete, all column types per database design
- `dotnet ef migrations add InitialCreate` produces a migration that creates both tables with correct DDL
- `dotnet ef database update` applies the migration against `(localdb)\MSSQLLocalDB` without errors
- Both tables visible in SSMS / VS SQL Server Object Explorer with correct columns and constraints

**Affected Layers:** Infrastructure, API (DI registration)

---

## Feature 3 — Stored Procedures: SaveDEXMeter, SaveDEXLaneMeter, ClearAllData

**Title:** Add the three stored procedures via a second EF Core migration

**Description:**  
Create a second migration (`AddStoredProcedures`) that adds the three SPs using `migrationBuilder.Sql()`. `SaveDEXMeter` upserts via `MERGE` and returns `DexMeterId` via `OUTPUT` parameter. `SaveDEXLaneMeter` inserts one lane row. `ClearAllData` deletes all rows from both tables with identity reseed. The `Down()` method drops all three SPs. Run the migration to verify SPs are created correctly.

**Acceptance Criteria:**
- `dotnet ef migrations add AddStoredProcedures` creates the migration file
- `Up()` creates all three SPs; `Down()` drops all three SPs
- `SaveDEXMeter` uses `MERGE` on `(Machine, DEXDateTime)` and returns the correct `DexMeterId` for both insert and update paths
- `SaveDEXLaneMeter` inserts exactly one row with the correct column values
- `ClearAllData` removes all rows from `DEXLaneMeter` then `DEXMeter` and reseeds identity to 0
- All three SPs are executable in SSMS with test parameters without errors
- Running the migration twice (up + down + up) leaves the database in the correct state

**Affected Layers:** Infrastructure (migration), Database

---

## Feature 4 — Domain Entities and Interfaces

**Title:** Define `DexMeter`, `DexLaneMeter` entities and the `IDexParserService` / `IDexRepository` interfaces

**Description:**  
In `VendSys.Domain`, create the two entity classes with properties matching the database columns. In `VendSys.Application`, create `DexMeterDto`, `DexLaneMeterDto`, `DexDocument` (the parse result), and the two interfaces: `IDexParserService` (takes raw string, returns `DexDocument`) and `IDexRepository` (two async write methods). No implementations yet.

**Acceptance Criteria:**
- `DexMeter` and `DexLaneMeter` entities in `VendSys.Domain` have no EF Core or framework references
- `DexMeterDto` fields: `Machine`, `DexDateTime`, `MachineSerialNumber`, `ValueOfPaidVends`
- `DexLaneMeterDto` fields: `ProductIdentifier`, `Price`, `NumberOfVends`, `ValueOfPaidSales`
- `DexDocument` holds one `DexMeterDto` and a `List<DexLaneMeterDto>`
- `IDexParserService` declares `Task<DexDocument> ParseAsync(string dexText)`
- `IDexRepository` declares `Task<int> SaveDexMeterAsync(DexMeterDto dto)` and `Task SaveDexLaneMeterAsync(int dexMeterId, DexLaneMeterDto dto)`
- All public types have XML doc comments (`///`)
- `dotnet build` passes

**Affected Layers:** Domain, Application

---

## Feature 5 — DEX Parser Service

**Title:** Implement `DexParserService` that extracts ID, VA, and all PA segments from a DEX string

**Description:**  
In `VendSys.Infrastructure/Parsing/DexParserService.cs`, implement `IDexParserService`. Split the input by newline, iterate segments. Extract `MachineSerialNumber` from `ID1[0]`, `DexDateTime` from `ID5[0]` + `ID5[1]` (combined with `DateTime.ParseExact`), and `ValueOfPaidVends` from `VA1[0]` (divide cents by 100). For each `PA1` segment, create a `DexLaneMeterDto`; populate it with price from `PA1[1]` (cents ÷ 100) and then read `PA2[0]` (NumberOfVends) and `PA2[1]` (ValueOfPaidSales, cents ÷ 100) from the immediately following `PA2` line. Throw `ArgumentException` on null/empty input. Throw `InvalidOperationException` on missing required segments.

**Acceptance Criteria:**
- `ParseAsync("") ` throws `ArgumentException`
- `ParseAsync(machineADexText)` returns `MachineSerialNumber == "100077238"`
- `ParseAsync(machineADexText)` returns `DexDateTime == new DateTime(2023, 12, 10, 23, 10, 0)`
- `ParseAsync(machineADexText)` returns `ValueOfPaidVends == 344.50m`
- `ParseAsync(machineADexText)` returns `Lanes.Count == 32` (matching PA1 count in file)
- Lane `"101"` has `Price == 3.25m`, `NumberOfVends == 4`, `ValueOfPaidSales == 13.00m`
- Removing the ID1 line from input throws `InvalidOperationException`
- Removing the VA1 line from input throws `InvalidOperationException`
- All existing backend NUnit parser tests pass

**Affected Layers:** Infrastructure, (Application — interface already defined)

---

## Feature 6 — Repository Implementation

**Title:** Implement `DexRepository` that calls `SaveDEXMeter` and `SaveDEXLaneMeter` via EF Core

**Description:**  
In `VendSys.Infrastructure/Repositories/DexRepository.cs`, implement `IDexRepository`. `SaveDexMeterAsync` builds `SqlParameter` objects (including one `OUTPUT` parameter for `@DexMeterId`), calls `context.Database.ExecuteSqlRawAsync("EXEC SaveDEXMeter ...")`, reads the output parameter value, and returns the integer. `SaveDexLaneMeterAsync` builds input-only `SqlParameter` objects and calls `ExecuteSqlRawAsync("EXEC SaveDEXLaneMeter ...")`. Register `DexRepository` as scoped in `Program.cs`.

**Acceptance Criteria:**
- `SaveDexMeterAsync` calls the SP with correct parameter names and returns the generated `DexMeterId`
- `SaveDexLaneMeterAsync` calls the SP with correct parameter names including the FK
- Calling both methods sequentially against LocalDB persists rows verifiable in SSMS
- `SqlParameter` direction is `Output` for `@DexMeterId` and `Input` for all others
- No direct `DbSet.Add` or `SaveChangesAsync` calls in `DexRepository`
- All existing repository NUnit tests pass

**Affected Layers:** Infrastructure, API (DI registration)

---

## Feature 7 — API Endpoint POST /vdi-dex

**Title:** Implement the `POST /vdi-dex` Minimal API endpoint

**Description:**  
In `VendSys.Api/Endpoints/DexEndpoints.cs`, map `POST /vdi-dex`. Read the `machine` query param; return 400 if absent or not `"A"`/`"B"`. Read the request body as a string; return 400 if empty. Delegate to `ProcessDexFileUseCase.ExecuteAsync(dexText, machine)`. Return 200 with the summary JSON response shape defined in `api-design.md`. Implement `ProcessDexFileUseCase` in `VendSys.Application` to orchestrate the parser and repository calls.

**Acceptance Criteria:**
- `POST /vdi-dex?machine=A` with valid DEX body and correct auth returns 200 with JSON matching the response shape
- `POST /vdi-dex` with no `machine` param returns 400 with `{ "error": "..." }`
- `POST /vdi-dex?machine=C` returns 400
- `POST /vdi-dex?machine=A` with empty body returns 400
- Successful call persists exactly one `DEXMeter` row and N `DEXLaneMeter` rows in LocalDB
- `lanesProcessed` in the response equals the count of PA segments in the submitted DEX file
- Endpoint decorated with `.RequireAuthorization()`

**Affected Layers:** Application (use case), API (endpoint)

---

## Feature 8 — HTTP Basic Auth Middleware

**Title:** Implement `BasicAuthHandler` and register it as the "Basic" authentication scheme

**Description:**  
In `VendSys.Api/Auth/BasicAuthHandler.cs`, extend `AuthenticationHandler<AuthenticationSchemeOptions>`. In `HandleAuthenticateAsync`, read the `Authorization` header, verify the scheme is `"Basic"`, base64-decode the credentials, split on the first `:`, and compare against `BasicAuth:Username` and `BasicAuth:Password` from `IConfiguration`. Return `AuthenticateResult.Fail(...)` for any mismatch. Override `HandleChallengeAsync` to add the `WWW-Authenticate: Basic realm="VendSys"` header before the 401 response. Register the scheme and `AddAuthorization()` in `Program.cs`.

**Acceptance Criteria:**
- `POST /vdi-dex` with correct credentials returns the endpoint's status (200 or 400), not 401
- `POST /vdi-dex` with wrong password returns 401 with `WWW-Authenticate` header
- `POST /vdi-dex` with no `Authorization` header returns 401 with `WWW-Authenticate` header
- `POST /vdi-dex` with `Bearer sometoken` returns 401
- Credentials are read from `appsettings.json`; no hardcoded strings in handler code
- All existing auth NUnit tests pass

**Affected Layers:** API

---

## Feature 9 — Global Error Handling Middleware

**Title:** Implement `GlobalExceptionMiddleware` that returns consistent JSON error responses

**Description:**  
In `VendSys.Api/Middleware/GlobalExceptionMiddleware.cs`, wrap `next(context)` in a try/catch. Catch `InvalidOperationException` (DEX parse failures) → 400 with `{ "error": message }`. Catch all other exceptions → 500 with `{ "error": "An unexpected error occurred.", "traceId": "..." }`. Register the middleware as the second item in the pipeline (after Serilog, before auth). Log the exception using the injected `ILogger` before writing the response.

**Acceptance Criteria:**
- Submitting a DEX file missing a required segment returns 400 with a descriptive `error` field
- A deliberately thrown unhandled exception returns 500 with `error` and `traceId` fields
- `traceId` in the 500 response matches the value in the Serilog log entry for that request
- 401 responses from `BasicAuthHandler` are unaffected (middleware does not intercept auth failures)
- Response `Content-Type` is `application/json` for all error responses

**Affected Layers:** API

---

## Feature 10 — Serilog Request Logging

**Title:** Configure Serilog with console and rolling-file sinks; enrich POST /vdi-dex logs with machine context

**Description:**  
Replace the default ASP.NET Core logger with Serilog in `Program.cs` using `UseSerilog()`. Configure a console sink and a rolling-file sink writing to `logs/api-.log` with daily rolling and 14-day retention. Add `UseSerilogRequestLogging()` as the first item in the middleware pipeline. Enrich the diagnostic context with `Machine` (from query param) and `RemoteIP`. Log level overrides suppress noisy `Microsoft.*` and `EF Core` messages below `Warning`.

**Acceptance Criteria:**
- Every request to `POST /vdi-dex` produces one structured log line with method, path, status code, and duration
- Log line includes `Machine` property (`"A"` or `"B"`) when the param is present
- A file named `logs/api-YYYYMMDD.log` is created and written to on each request
- EF Core SQL queries do not appear in logs at `Information` level
- Log output is visible in the console when running `dotnet run`
- `appsettings.Development.json` sets minimum level to `Debug`

**Affected Layers:** API

---

## Feature 11 — Dockerfile and docker-compose for the API

**Title:** Create a multi-stage `Dockerfile` for `VendSys.Api` and a `docker-compose.yml`

**Description:**  
Add a `Dockerfile` to `src/VendSys.Api/` using a two-stage build: SDK stage for restore and publish; ASP.NET runtime stage for the final image. Expose port 8080. Add `docker-compose.yml` at the solution root with a single `VendSys-api` service. Mount `./logs` to `/app/logs`. Override the connection string via environment variable to use `host.docker.internal` for the LocalDB on the host. Document the Docker startup steps in a comment block at the top of `docker-compose.yml`.

**Acceptance Criteria:**
- `docker build -f src/VendSys.Api/Dockerfile -t VendSys-api .` completes without errors
- `docker-compose up` starts the API container and it responds to `GET /` (or health probe) on port 8080
- `POST /vdi-dex` from `curl` or Postman to `http://localhost:8080/vdi-dex` reaches the endpoint
- Log files appear in `./logs/` on the host after a request
- The Dockerfile uses `mcr.microsoft.com/dotnet/sdk:9.0` and `mcr.microsoft.com/dotnet/aspnet:9.0` base images
- The final image does not include the SDK layer or source files

**Affected Layers:** API (infrastructure/deployment)

---

## Feature 12 — Backend NUnit Tests

**Title:** Write and pass all NUnit tests for `DexParserService`, `BasicAuthHandler`, and `DexRepository`

**Description:**  
Implement all test cases defined in `testing-strategy.md` sections 4.1, 4.2, and 4.3. Use NSubstitute for mocking. DEX sample strings for parser tests are embedded as string constants in the test project (copied from `/Docs/DEX Machine A.txt` and `/Docs/DEX Machine B.txt`). Auth handler tests use a `TestServer`. Repository tests verify SP call parameters via a custom interceptor or a thin wrapper interface.

**Acceptance Criteria:**
- All tests in `tests/VendSys.Tests/` pass: `dotnet test tests/VendSys.Tests/`
- Parser tests: 14 cases — all pass
- Auth handler tests: 7 cases — all pass
- Repository tests: 12 cases — all pass
- No test makes a real network or database call
- Test project builds without warnings

**Affected Layers:** Tests (covering Infrastructure and API)

---

## Feature 13 — MAUI Project Setup and DI (MauiProgram.cs)

**Title:** Scaffold `VendSys.Maui`, configure `MauiProgram.cs` with all service registrations and Polly

**Description:**  
Create the MAUI project targeting Windows, Android, and iOS. Add `CommunityToolkit.Maui`, `CommunityToolkit.Mvvm`, and `Microsoft.Extensions.Http.Polly`. In `MauiProgram.cs`, register `IDexFileService`, `IApiService`, `MainViewModel`, `MainPage`, and the named `HttpClient` with the Polly retry policy (3 retries, exponential backoff). Create `ApiConstants.cs` with all string constants. Verify the app launches on Windows without crashing.

**Acceptance Criteria:**
- App builds and launches on Windows (WinUI 3) without runtime errors
- `MauiProgram.cs` registers all services, the named `HttpClient`, and Polly policy
- `ApiConstants` contains `HttpClientName`, `BaseUrl`, `DexEndpoint`, `MachineParamKey`, `MachineA`, `MachineB`, `AuthUsername`, `AuthPassword`, `AuthScheme` — all as `const string`
- No string literals appear outside `ApiConstants` for API-related values
- `UseMauiCommunityToolkit()` is called in the builder chain
- `dotnet build` passes for all three target frameworks

**Affected Layers:** MAUI

---

## Feature 14 — Embedded DEX Resources and DexFileService

**Title:** Embed the two DEX sample files as `EmbeddedResource` and implement `DexFileService`

**Description:**  
Copy `DEX Machine A.txt` and `DEX Machine B.txt` from `/Docs/` to `src/VendSys.Maui/Resources/Raw/` as `MachineA.txt` and `MachineB.txt`. Mark both as `EmbeddedResource` in the `.csproj` with explicit `LogicalName` values. Implement `DexFileService` to load the correct file via `Assembly.GetManifestResourceStream()`. Throw `InvalidOperationException` with a clear message if the resource key is not found.

**Acceptance Criteria:**
- `Assembly.GetExecutingAssembly().GetManifestResourceNames()` lists both resource keys
- `DexFileService.LoadDexFile("A")` returns the full text of `MachineA.txt`
- `DexFileService.LoadDexFile("B")` returns the full text of `MachineB.txt`
- `DexFileService.LoadDexFile("C")` throws `InvalidOperationException`
- The loaded strings start with `DXS*` (the DEX envelope header)
- No file system path is used at runtime; loading works on Android and iOS

**Affected Layers:** MAUI (Services, Resources)

---

## Feature 15 — HttpClient and Polly Retry Policy

**Title:** Implement `ApiService` with HTTP Basic Auth header and verify Polly retry wires correctly

**Description:**  
Implement `ApiService` using `IHttpClientFactory.CreateClient(ApiConstants.HttpClientName)`. Build the `Authorization: Basic` header per-request from `ApiConstants`. Construct the POST request with `text/plain` content to `ApiConstants.DexEndpoint?machine={machine}`. Map response status to `ApiResult`. Handle `HttpRequestException` as a failure result. Verify in a test that Polly retries on 503.

**Acceptance Criteria:**
- `ApiService.SendDexFileAsync("A", content)` sends a POST to `/vdi-dex?machine=A`
- The `Authorization` header decodes to `vendsys:NFsZGmHAGWJSZ#RuvdiV`
- `Content-Type` is `text/plain; charset=utf-8`
- A 200 response returns `ApiResult.IsSuccess == true`
- A 500 response returns `ApiResult.IsSuccess == false` with status code in `ErrorMessage`
- An `HttpRequestException` returns `ApiResult.IsSuccess == false`
- Polly retries exactly 3 times on 503 before returning the failure result
- All ApiService NUnit tests pass

**Affected Layers:** MAUI (Services)

---

## Feature 16 — MainViewModel and Commands

**Title:** Implement `MainViewModel` with `SendDexACommand`, `SendDexBCommand`, `IsBusy`, and error event

**Description:**  
Implement `MainViewModel` extending `ObservableObject`. Declare `IsBusy`, `StatusMessage`, and `IsError` as observable properties. Wire `SendDexACommand` and `SendDexBCommand` as `AsyncRelayCommand` instances with `CanExecute = () => !IsBusy`. In `SendDexFileAsync`, set `IsBusy = true`, call `IDexFileService.LoadDexFile` and `IApiService.SendDexFileAsync`, update `StatusMessage`/`IsError` based on result, then set `IsBusy = false`. Call `NotifyCanExecuteChanged()` on both commands when `IsBusy` changes. Raise `OnSendFailed` event on failure.

**Acceptance Criteria:**
- `IsBusy` is `true` while the async command is executing
- Both command `CanExecute()` return `false` while `IsBusy` is `true`
- Both commands re-enable after the call completes (success or failure)
- `StatusMessage` is non-empty after each call
- `IsError` is `true` only on failure
- `OnSendFailed` event is raised with the error message on API failure
- All MainViewModel NUnit tests pass

**Affected Layers:** MAUI (ViewModels)

---

## Feature 17 — MainPage UI (XAML)

**Title:** Build `MainPage.xaml` with two buttons, activity indicator, status label, and resource-dictionary styles

**Description:**  
Implement `MainPage.xaml` as a `ContentPage` with a `VerticalStackLayout` (padding 16, spacing 24). Add a heading label, a subtitle label, two styled `Button` controls bound to `SendDexACommand` and `SendDexBCommand`, an `ActivityIndicator` bound to `IsBusy`, and a status `Label` bound to `StatusMessage`. Apply `AutomationProperties.Name` and `AutomationProperties.HelpText` on both buttons. Define all colours in `Colors.xaml` and all button/text styles in `Styles.xaml` — no hex literals in the page. Implement `BoolToColorConverter` and `StringToBoolConverter` in code-behind, registered in `App.xaml`.

**Acceptance Criteria:**
- App launches and shows both buttons on Windows, Android emulator
- Tapping Button A while API is unreachable shows an error `StatusMessage` and sets `IsError = true`
- While a button command is executing, both buttons are visually disabled and the `ActivityIndicator` spins
- After the command completes, both buttons are re-enabled
- No hex colour values (`#RRGGBB`) appear in `MainPage.xaml`
- All interactive elements have `AutomationProperties.Name` set
- App handles portrait and landscape orientations without layout overflow

**Affected Layers:** MAUI (Views, Resources)

---

## Feature 18 — MAUI NUnit Tests

**Title:** Write and pass all NUnit tests for `MainViewModel` and `ApiService`

**Description:**  
Implement all test cases defined in `testing-strategy.md` sections 5.1 and 5.2. Use NSubstitute for `IApiService` and `IDexFileService`. For `ApiService` tests, inject a `MockHttpMessageHandler` to capture outgoing requests and return controlled responses. Include Polly retry tests by making the mock return 503 a configurable number of times before returning 200.

**Acceptance Criteria:**
- All tests in `tests/VendSys.Maui.Tests/` pass: `dotnet test tests/VendSys.Maui.Tests/`
- `MainViewModelTests`: 13 cases — all pass
- `ApiServiceTests`: 11 cases — all pass
- No test relies on a real HTTP server or file system
- Test project builds without warnings

**Affected Layers:** Tests (covering MAUI ViewModels and Services)

---

## Feature 19 — README.md

**Title:** Write the project `README.md` with setup, run, and test instructions

**Description:**  
Create a `README.md` at the repo root covering: project overview, prerequisites (`dotnet 9`, `SQL Server LocalDB`, `Docker Desktop`), how to apply EF Core migrations, how to run the API locally and via Docker, how to configure and run the MAUI app (and how to point it at a non-localhost URL for a physical device), how to run all NUnit tests, and a section documenting the AI-assisted development process and commentary as required by the challenge brief.

**Acceptance Criteria:**
- A developer who has not seen this project before can follow the README to get the API running and receive a 200 from `POST /vdi-dex` within 15 minutes
- All `dotnet` commands in the README execute without modification on a clean Windows machine with the stated prerequisites
- The Docker section explains the `host.docker.internal` connection string override
- The AI process section contains the full prompt history and a short commentary paragraph on code quality and AI output style
- README is written in clear English with no broken links

**Affected Layers:** Documentation

---

## Implementation Order Summary

```
 1. Solution scaffolding          → foundation for everything
 2. EF Core + DbContext           → schema ownership established
 3. Stored procedures             → write path defined in DB
 4. Domain entities + interfaces  → contracts set, no implementations
 5. DEX parser service            → core parsing logic
 6. Repository implementation     → SP execution wired up
 7. API endpoint                  → full backend path works end-to-end
 8. Basic Auth middleware          → endpoint secured
 9. Error handling middleware      → consistent error surface
10. Serilog logging               → observability
11. Docker                        → deployable
12. Backend tests                 → backend verified
13. MAUI scaffolding + DI         → MAUI project ready
14. DEX resources + file service  → DEX content loadable
15. HttpClient + Polly            → HTTP transport ready
16. ViewModel                     → MAUI logic complete
17. XAML UI                       → full MAUI app usable
18. MAUI tests                    → MAUI verified
19. README                        → submission ready
```
