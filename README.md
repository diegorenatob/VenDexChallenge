# VendSys Challenge

A full-stack system for receiving, parsing, and persisting DEX vending machine data. Built with ASP.NET Core 9 Minimal API, SQL Server, and .NET MAUI.

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Prerequisites](#2-prerequisites)
3. [Solution Structure](#3-solution-structure)
4. [Database Setup](#4-database-setup)
5. [Running the API Locally](#5-running-the-api-locally)
6. [Running the API with Docker](#6-running-the-api-with-docker)
7. [Running the MAUI App](#7-running-the-maui-app)
8. [Running the Tests](#8-running-the-tests)
9. [AI-Assisted Development](#9-ai-assisted-development)

---

## 1. Project Overview

The VendSys system receives DEX (Data Exchange) files from vending machines via a .NET MAUI desktop/mobile app and stores structured sales data in a SQL Server database through a secured REST API.

**Flow:**

```
MAUI App  →  POST /vdi-dex?machine=A  →  ASP.NET Core API  →  SQL Server
              (DEX text body, Basic Auth)    (parse + upsert)
```

**Key features:**
- DEX parser extracts machine-level and per-lane sales data from raw segment text
- `SaveDEXMeter` and `SaveDEXLaneMeter` stored procedures handle upsert and insert
- HTTP Basic Auth secures every endpoint (credentials in `appsettings.json`)
- Polly retry policy (3 retries, exponential backoff) on the MAUI HTTP client
- Clear All Data endpoint (`DELETE /vdi-dex/clear`) with confirmation dialog in the MAUI app
- Serilog structured logging to console and rolling daily file

---

## 2. Prerequisites

| Requirement | Version | Notes |
|---|---|---|
| .NET SDK | 9.0 | [Download](https://dotnet.microsoft.com/download/dotnet/9.0) |
| SQL Server LocalDB | 2019+ | Included with Visual Studio; or install [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) |
| .NET MAUI workload | 9.0 | Run `dotnet workload install maui` |
| Docker Desktop | 4.x+ | Only required for the Docker section |

Verify your environment:

```bash
dotnet --version        # should be 9.x.x
dotnet ef --version     # should be 9.x.x
sqllocaldb info         # should list MSSQLLocalDB
```

Install the EF Core CLI tools globally if not already present:

```bash
dotnet tool install --global dotnet-ef
```

---

## 3. Solution Structure

```
VendSysChallenge/
├── src/
│   ├── Backend/
│   │   ├── VendSys.Domain/           # Entities (no framework dependencies)
│   │   ├── VendSys.Application/      # Interfaces, DTOs, use cases
│   │   ├── VendSys.Infrastructure/   # EF Core, parser, repository, migrations
│   │   └── VendSys.Api/              # Minimal API, auth, middleware, DI root
│   └── Client/
│       ├── VendSys.Maui.Application/ # Client interfaces and models
│       ├── VendSys.Maui.Infrastructure/ # ApiService, DexFileService
│       └── VendSys.Maui/             # MAUI app (views, viewmodels, DI root)
├── tests/
│   ├── Backend/VendSys.Api.Tests/    # NUnit: parser, auth, repository, endpoint
│   └── Client/VendSys.Maui.Tests/   # NUnit: ApiService, MainViewModel
├── Docs/                             # Architecture docs, DEX sample files
├── docker-compose.yml
└── VendSys.sln
```

---

## 4. Database Setup

### 4.1 Apply Migrations

From the repository root, run both EF Core migrations to create the `VendSys` database, both tables, and the three stored procedures:

```bash
dotnet ef database update \
  --project src/Backend/VendSys.Infrastructure \
  --startup-project src/Backend/VendSys.Api
```

This applies:
- `InitialCreate` — creates `DEXMeter` and `DEXLaneMeter` tables
- `AddStoredProcedures` — creates `SaveDEXMeter`, `SaveDEXLaneMeter`, and `ClearAllData` stored procedures

The default connection string targets `(localdb)\MSSQLLocalDB` and is configured in `src/Backend/VendSys.Api/appsettings.json`.

### 4.2 Verify

Connect to `(localdb)\MSSQLLocalDB` in SSMS or Visual Studio SQL Server Object Explorer and confirm:
- Database `VendSys` exists
- Tables `dbo.DEXMeter` and `dbo.DEXLaneMeter` exist
- Stored procedures `dbo.SaveDEXMeter`, `dbo.SaveDEXLaneMeter`, and `dbo.ClearAllData` exist

---

## 5. Running the API Locally

```bash
dotnet run --project src/Backend/VendSys.Api
```

The API listens on `http://localhost:5000` by default.

### 5.1 Test the Endpoint

Send a DEX file using `curl` (replace `<dex-text>` with the contents of `Docs/DEX Machine A.txt`):

```bash
curl -X POST "http://localhost:5000/vdi-dex?machine=A" \
  -H "Authorization: Basic dmVuZHN5czpORnNaR21IQUdXSlNaI1J1dmRpVg==" \
  -H "Content-Type: text/plain" \
  --data-binary @"Docs/DEX Machine A.txt"
```

Expected response (200 OK):

```json
{
  "machine": "A",
  "dexDateTime": "2023-12-10T23:10:00",
  "machineSerialNumber": "100077238",
  "valueOfPaidVends": 344.50,
  "lanesProcessed": 32
}
```

### 5.2 Authentication

All endpoints require HTTP Basic Auth:

| Field | Value |
|---|---|
| Username | `vendsys` |
| Password | `NFsZGmHAGWJSZ#RuvdiV` |

Base64 encoded: `dmVuZHN5czpORnNaR21IQUdXSlNaI1J1dmRpVg==`

### 5.3 Clear All Data

```bash
curl -X DELETE "http://localhost:5000/vdi-dex/clear" \
  -H "Authorization: Basic dmVuZHN5czpORnNaR21IQUdXSlNaI1J1dmRpVg=="
```

Returns `204 No Content` on success.

---

## 6. Running the API with Docker

> **Note:** SQL Server LocalDB uses Windows Authentication over a named pipe and is not reachable from a Linux container. You need a full SQL Server instance that accepts TCP connections with a SQL login (e.g. SQL Server Express, SQL Server Developer, or Azure SQL).

### 6.1 Configure the Connection String

Edit `docker-compose.yml` and replace `<YourPassword>` with your SQL Server `sa` password:

```yaml
ConnectionStrings__DefaultConnection: "Server=host.docker.internal,1433;Database=VendSys;User Id=sa;Password=<YourPassword>;MultipleActiveResultSets=False;TrustServerCertificate=True;"
```

`host.docker.internal` resolves to the host machine from inside the container, allowing the Linux container to reach SQL Server running on Windows.

### 6.2 Apply Migrations Against the TCP Instance

Before starting Docker, apply migrations to the TCP-accessible SQL Server instance:

```bash
dotnet ef database update \
  --project src/Backend/VendSys.Infrastructure \
  --startup-project src/Backend/VendSys.Api \
  --connection "Server=localhost,1433;Database=VendSys;User Id=sa;Password=<YourPassword>;TrustServerCertificate=True;"
```

### 6.3 Build and Run

```bash
docker-compose up --build
```

The API is available at `http://localhost:8080`. Logs are written to `./logs/` on the host.

```bash
curl -X POST "http://localhost:8080/vdi-dex?machine=A" \
  -H "Authorization: Basic dmVuZHN5czpORnNaR21IQUdXSlNaI1J1dmRpVg==" \
  -H "Content-Type: text/plain" \
  --data-binary @"Docs/DEX Machine A.txt"
```

### 6.4 Manual Docker Build

```bash
docker build -f src/Backend/VendSys.Api/Dockerfile -t VendSys-api .
docker run -p 8080:8080 \
  -e "ConnectionStrings__DefaultConnection=Server=host.docker.internal,1433;Database=VendSys;User Id=sa;Password=<YourPassword>;TrustServerCertificate=True;" \
  VendSys-api
```

---

## 7. Running the MAUI App

### 7.1 Prerequisites

Install the MAUI workload if you have not already:

```bash
dotnet workload install maui
```

### 7.2 Run on Windows

```bash
dotnet run --project src/Client/VendSys.Maui --framework net9.0-windows10.0.19041.0
```

Or open `VendSys.sln` in Visual Studio 2022 (17.8+), set `VendSys.Maui` as the startup project, select the **Windows Machine** target, and press F5.

### 7.3 Pointing the App at a Non-Localhost URL

By default the app targets `http://localhost:5000` (defined in `src/Client/VendSys.Maui.Infrastructure/ApiConstants.cs`).

To run against a remote server or Docker container from a physical Android/iOS device:

1. Open `src/Client/VendSys.Maui.Infrastructure/ApiConstants.cs`
2. Update `BaseUrl` to the reachable address, e.g.:
   ```csharp
   public const string BaseUrl = "http://192.168.1.100:8080";
   ```
3. On Android, HTTP (non-TLS) requires adding a network security config or targeting a local dev server with `ClearTextTrafficPermitted`. For production, use HTTPS.

### 7.4 App Features

| Button | Action |
|---|---|
| **Send Machine A** | Loads embedded `MachineA.txt`, POSTs to `/vdi-dex?machine=A` |
| **Send Machine B** | Loads embedded `MachineB.txt`, POSTs to `/vdi-dex?machine=B` |
| **Clear All Data** | Shows confirmation dialog, then sends `DELETE /vdi-dex/clear` |

A status label below the buttons shows success or error messages. All three buttons are disabled while a request is in flight.

---

## 8. Running the Tests

### 8.1 All Tests

```bash
dotnet test tests/Backend/VendSys.Api.Tests/VendSys.Api.Tests.csproj
dotnet test tests/Client/VendSys.Maui.Tests/VendSys.Maui.Tests.csproj
```

### 8.2 Backend Tests (36 cases)

| Suite | Cases | Covers |
|---|---|---|
| `DexParserServiceTests` | 14 | Segment extraction, edge cases, error paths |
| `BasicAuthHandlerTests` | 7 | Valid credentials, wrong password, missing header, wrong scheme |
| `DexRepositoryTests` | 12 | SP parameter binding, output parameter, ClearAllData |
| `DexEndpointsTests` | 3 | 400 on bad machine, 400 on empty body, 204 on clear |

### 8.3 Client Tests (16 cases)

| Suite | Cases | Covers |
|---|---|---|
| `ApiServiceTests` | 11 | Auth header, 200/500/exception mapping, Polly 3-retry on 503 |
| `MainViewModelTests` | 5 | IsBusy toggling, CanExecute, StatusMessage, IsError, ClearTables confirmation |

No test makes a real network or database call. The backend tests use `WebApplicationFactory<Program>` with a fake SQL executor; the MAUI tests use NSubstitute mocks and a `CapturingHandler` for HTTP.

---

## 9. AI-Assisted Development

This project was built using [Claude Code](https://claude.ai/code) as a development accelerator within a carefully structured foundation. Rather than allowing the assistant to generate the full project structure from scratch — a common source of architectural drift and AI-introduced errors — I established the architecture upfront, created a comprehensive backlog, and leveraged SDK templates as the starting point. Claude Code then worked like a developer adding features to an existing codebase, rather than creating one from nothing.

### Project Setup & Architecture

Before writing any code, I defined the complete architecture: layered separation (API, Domain, Infrastructure, Presentation), library selection (.NET 9, MAUI, NUnit, Polly, Community Toolkit), and naming conventions. I created a comprehensive backlog in `Docs/backlog.md` that served as a specification for each feature.

Rather than letting Claude Code generate projects from scratch — which often leads to idiosyncratic folder structures, unnecessary abstractions, and scaffolding bloat — I used official SDK templates (dotnet new) to create each project with standard conventions already in place. This provided a clean foundation that matched industry expectations. I then deleted all unnecessary scaffolding files, leaving only the directory structure and base classes needed.

Claude Code worked on top of these curated templates, adding features and classes as if it were a regular developer joining an existing team. This constraint eliminated entire classes of AI errors: no exotic folder layouts, no invented design patterns, and no half-baked examples cluttering the codebase.

### Development Workflow

For each feature in the backlog, I'd describe the requirements and point Claude to the relevant files or layers. The assistant would implement the feature—adding classes, methods, tests, and migrations—while respecting the existing structure. I then performed code review before every commit, examining the generated code for architectural correctness, performance implications, adherence to conventions, and potential edge cases.

This meant catching issues early — such as unnecessary allocations, missing null checks, or patterns that didn't align with the backend's Clean Architecture. Once corrections were made (either by requesting changes from the assistant or by manual editing), the code was committed with confidence that it met the project's standards.

### Key Technical Decisions

**MVVM Framework Selection**  
The initial implementation used a custom `INotifyPropertyChanged` wrapper, which I rejected in favor of `CommunityToolkit.Mvvm`. The assistant quickly refactored to use `[ObservableProperty]` and `[RelayCommand]` attributes throughout the MAUI layer, reducing boilerplate considerably.

**Namespace Conflict Resolution**  
MAUI's built-in `Application` class created an ambiguity with our `VendSys.Maui.Application` layer. Rather than using fully qualified names everywhere, I asked the assistant to rename the application layer to `VendSys.Client.Application`. The refactoring was applied consistently across all files and imports.

**Converter Cleanup**  
When I questioned whether custom `BoolToColorConverter` and `StringToBoolConverter` implementations were necessary, the assistant confirmed that `CommunityToolkit.MVVM` already provides `BoolToObjectConverter` and `IsStringNotNullOrEmptyConverter`. The custom implementations were removed, simplifying the codebase.

**Clean Architecture for the Frontend**  
I requested that the MAUI project mirror the backend's layered structure with Application, Infrastructure, and Presentation concerns. The assistant restructured the project and correctly implemented the `Assembly` injection pattern to load embedded DEX resources from the Infrastructure layer — a non-obvious detail that required understanding MAUI's resource loading behavior.

**High-Volume Data Insertion Optimization**  
Initially, seed data was inserted using Entity Framework's standard `AddRange()` and `SaveChanges()`. When profiling revealed insertion times of **8–12 seconds** for ~500k records, I refactored to raw ADO.NET with bulk insert patterns. I created an `ISqlInjector` interface to abstract the data insertion layer, replacing the EF approach with parameterized SQL batch operations. This reduced insertion time to **300–400ms** — a **25–40x improvement** — which was critical for Docker initialization and testing workflows.

### Code Quality and Style

Claude Code produces idiomatic, production-quality C# that adheres to the project's conventions: file-scoped namespaces, `_prefix` field naming, `///` XML doc comments, and `Async` method suffixes. The assistant also proactively flagged potential issues — notably the LocalDB/Docker TCP compatibility limitation and the requirement for `x:DataType` on compiled bindings to suppress XAML warnings.

The codebase reads as though it was written by a consistent hand, with no noticeable shifts in style or approach across different files and layers.
