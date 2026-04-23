# VenDex Challenge

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

The VenDex system receives DEX (Data Exchange) files from vending machines via a .NET MAUI desktop/mobile app and stores structured sales data in a SQL Server database through a secured REST API.

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
VenDexChallenge/
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

From the repository root, run both EF Core migrations to create the `VenDex` database, both tables, and the three stored procedures:

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
- Database `VenDex` exists
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
ConnectionStrings__DefaultConnection: "Server=host.docker.internal,1433;Database=VenDex;User Id=sa;Password=<YourPassword>;MultipleActiveResultSets=False;TrustServerCertificate=True;"
```

`host.docker.internal` resolves to the host machine from inside the container, allowing the Linux container to reach SQL Server running on Windows.

### 6.2 Apply Migrations Against the TCP Instance

Before starting Docker, apply migrations to the TCP-accessible SQL Server instance:

```bash
dotnet ef database update \
  --project src/Backend/VendSys.Infrastructure \
  --startup-project src/Backend/VendSys.Api \
  --connection "Server=localhost,1433;Database=VenDex;User Id=sa;Password=<YourPassword>;TrustServerCertificate=True;"
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
docker build -f src/Backend/VendSys.Api/Dockerfile -t vendex-api .
docker run -p 8080:8080 \
  -e "ConnectionStrings__DefaultConnection=Server=host.docker.internal,1433;Database=VenDex;User Id=sa;Password=<YourPassword>;TrustServerCertificate=True;" \
  vendex-api
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

This project was built end-to-end with [Claude Code](https://claude.ai/code) (Anthropic's AI coding assistant) as the primary development tool. The full prompt history is available in the conversation export provided with this submission.

### Development Process

The project was implemented feature by feature, following the backlog in `Docs/backlog.md`. Each feature was described to Claude Code in natural language, and the assistant generated the full implementation — project scaffolding, C# classes, XAML, SQL migrations, NUnit tests, and Docker configuration — with no manual code editing outside of targeted corrections.

Notable interactions:

- **Architecture corrections:** When I asked for MVVM in the MAUI layer, the initial implementation used custom `INotifyPropertyChanged`. After clarifying that I wanted `CommunityToolkit.Mvvm`, the assistant refactored to `[ObservableProperty]` and `[RelayCommand]` attributes throughout.
- **Namespace conflict resolution:** The assistant identified and resolved a namespace clash between `VendSys.Maui.Application` and MAUI's built-in `Application` class, renaming the client Application layer to `VendSys.Client.Application` across all files.
- **Converter discovery:** When I asked whether the custom `BoolToColorConverter` and `StringToBoolConverter` existed in the Community Toolkit, the assistant confirmed they do (`BoolToObjectConverter` and `IsStringNotNullOrEmptyConverter`) and deleted the custom implementations.
- **DDD refactor:** At my request, the MAUI project was restructured into Application, Infrastructure, and Presentation layers — mirroring the backend's Clean Architecture — including the correct `Assembly` injection pattern to load embedded DEX resources from the Infrastructure layer.

### Commentary on AI Output Style

Claude Code produces idiomatic, production-quality C# that follows the project's stated conventions (file-scoped namespaces, `_prefix` fields, `///` doc comments, `Async` suffix) consistently across all files. It defaults to the simplest correct approach and does not add speculative features or abstractions beyond the task at hand.

The assistant handled cross-cutting concerns (Polly wiring, DI registration, middleware ordering, compiled bindings) correctly on the first attempt, and proactively warned about potential issues such as the LocalDB/Docker TCP compatibility limitation and the `x:DataType` compiled binding requirement for suppressing XAML warnings.

The main area requiring human correction was MVVM toolkit selection — the assistant made a reasonable assumption about which toolkit to use when the instruction was ambiguous. Once the preference was stated clearly, all subsequent MVVM code was generated correctly and consistently.

Overall, AI assistance reduced the implementation time for this project significantly while maintaining code quality that would pass a standard pull request review.
