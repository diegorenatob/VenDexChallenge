# VenDex Challenge — Project Context

## 1. Project Overview

**Client:** Nayax / VendSys  
**Contact:** Hannah Davies (hannah.davies@vendsys.com)  
**Goal:** Demonstrate proficiency in C# .NET, ASP.NET Core Minimal APIs, Microsoft SQL Server, and cross-platform app development using .NET MAUI.

The project involves building a full stack system that:
1. Receives DEX vending machine data files via a MAUI mobile/desktop app
2. Processes and stores that data through a REST API
3. Persists structured sales data in a SQL Server database

---

## 2. What is DEX?

DEX (Data Exchange) is a standard protocol used by unattended retail devices (vending machines) to export machine data. The format is defined by the NAMA (National Automatic Merchandising Association) DEX specification.

A DEX file is a line-delimited text file where each line is a **segment** of the form:

```
SEGMENT_ID*field1*field2*field3...
```

Fields are delimited by `*`. Empty fields are represented by consecutive `**`.

### 2.1 Segment Structure

#### Header / Machine-Level Segments (appear once per file)

| Segment | Field Position | Name | Example Value |
|---------|---------------|------|---------------|
| `DXS`  | [1] | Sender ID | `STF0000000` |
| `DXS`  | [2] | Communication ID | `VA` |
| `DXS`  | [3] | Version | `V0/6` |
| `ST`   | [1] | Transaction Set ID | `001` |
| `ID1`  | [1] | **MachineSerialNumber** (ID101, Appendix A p.30) | `100077238` |
| `ID1`  | [2] | Location/Route ID | `187` |
| `ID1`  | [5] | Location Name | `Hill Hall` |
| `ID4`  | [1] | Number of currencies | `2` |
| `ID4`  | [2] | Currency exponent | `1840` (Machine A) / `840` (Machine B) |
| `ID4`  | [3] | Currency code | `USD` |
| `ID5`  | [1] | **DEX Date** (YYYYMMDD) | `20231210` |
| `ID5`  | [2] | **DEX Time** (HHMM) | `2310` |
| `VA1`  | [1] | **ValueOfPaidVends** (VA101, Appendix A p.45) | `34450` (in cents) |

#### Per-Lane / Per-Product Segments (repeat for each lane)

Each lane starts with a `PA1` segment and is followed by `PA2`, `PA3`, `PA4`, `PA5`, `PA7`, `PA8`.

| Segment | Field Position | Name | Example Value |
|---------|---------------|------|---------------|
| `PA1`  | [1] | **ProductIdentifier** (PA101, Appendix A p.36) | `101` |
| `PA1`  | [2] | **Price** (PA102) | `325` (in cents = $3.25) |
| `PA2`  | [1] | **NumberOfVends** (PA201) | `4` |
| `PA2`  | [2] | **ValueOfPaidSales** (PA202) | `1300` (in cents) |
| `PA5`  | [1] | Date of last sale (YYYYMMDD) | `20231201` |
| `PA5`  | [2] | Time of last sale (HHMMSS) | `121501` |

#### Segments to Ignore

All other segments (`CA`, `BA`, `TA`, `DA`, `EA`, `MA`, `CB`, `G85`, `SE`, `DXE`) are out of scope for this challenge.

### 2.2 Sample File Summary

**DEX Machine A.txt:**
- MachineSerialNumber: `100077238`
- DEXDateTime: `2023-12-10 23:10`
- ValueOfPaidVends: `34450` cents ($344.50)
- Lanes: 101, 103, 105, 107, 201, 203, 205, 207, 301, 303, 305, 307, 401–408, 501–508, 601, 603, 605, 607, 702–707

**DEX Machine B.txt:**
- MachineSerialNumber: `302029479`
- DEXDateTime: `2023-12-10 23:11`
- ValueOfPaidVends: `475885` cents ($4,758.85)
- Lanes: 101, 103, 105, 107, 201, 203, 205, 207, 301, 303, 305, 307, 401–408, 501–508, 601–607

---

## 3. Database Schema

### 3.1 Table: DEXMeter

Stores one record per DEX file received (machine-level data).

| Column | Data Type | Constraints | Source |
|--------|-----------|-------------|--------|
| `Id` | `INT IDENTITY(1,1)` | PRIMARY KEY | Auto |
| `Machine` | `CHAR(1)` | NOT NULL | API request parameter |
| `DEXDateTime` | `DATETIME` | NOT NULL, UNIQUE per machine | ID5[1] + ID5[2] |
| `MachineSerialNumber` | `VARCHAR(50)` | NOT NULL | ID1[1] (ID101) |
| `ValueOfPaidVends` | `INT` | NOT NULL | VA1[1] (VA101), in cents |

**Note:** The challenge states DEXDateTime will be unique for each machine. A composite unique constraint on `(Machine, DEXDateTime)` enforces this.

### 3.2 Table: DEXLaneMeter

Stores one record per lane per DEX file (lane-level sales data).

| Column | Data Type | Constraints | Source |
|--------|-----------|-------------|--------|
| `Id` | `INT IDENTITY(1,1)` | PRIMARY KEY | Auto |
| `DEXMeterId` | `INT` | NOT NULL, FK → DEXMeter.Id | Foreign key |
| `ProductIdentifier` | `VARCHAR(20)` | NOT NULL | PA1[1] (PA101) |
| `Price` | `INT` | NOT NULL | PA1[2] (PA102), in cents |
| `NumberOfVends` | `INT` | NOT NULL | PA2[1] (PA201) |
| `ValueOfPaidSales` | `INT` | NOT NULL | PA2[2] (PA202), in cents |

**Relationship:** DEXLaneMeter.DEXMeterId → DEXMeter.Id (many-to-one)

### 3.3 Stored Procedures

Option chosen: **Two stored procedures** (parse in C#, call separately):
- `SaveDEXMeter` — inserts a DEXMeter record and returns the generated Id
- `SaveDEXLaneMeter` — inserts a DEXLaneMeter record linked to a DEXMeter Id

---

## 4. Technical Requirements

### 4.1 .NET MAUI Application
- Two buttons: **Button A** and **Button B**
- Each button sends the corresponding DEX file text as HTTP POST body to the API
- DEX file text hardcoded as string fields in the app
- Endpoint: `POST http://<host>/vdi-dex` with query parameter `machine=A` or `machine=B`
- Must include HTTP Basic Authorization header: `vendsys:NFsZGmHAGWJSZ#RuvdiV`

### 4.2 ASP.NET Core 9 Minimal API
- Single endpoint: `POST /vdi-dex`
- Accepts DEX file as plain text in the HTTP request body
- Accepts `machine` query parameter (`A` or `B`)
- Implements HTTP Basic Authorization
  - Username: `vendsys`
  - Password: `NFsZGmHAGWJSZ#RuvdiV`
  - Credentials stored in `appsettings.json` (no Users table needed)
- Parses the DEX text in C# and calls stored procedures
- Should be fast and support concurrent requests

### 4.3 SQL Server Database
- SQL Server LocalDB or SQL Server Express
- Two tables: `DEXMeter` and `DEXLaneMeter`
- Linked via foreign key
- Submit includes `.bak` database backup

### 4.4 Docker (implied by challenge context)
- The API should be containerizable for local/network accessibility from the MAUI app

### 4.5 Code Quality
- Clean, readable, well-commented code
- Full AI chat history must be included with the submission
- Written analysis/commentary on AI output style and appropriateness

---

## 5. DEX Parsing Rules

When parsing the DEX text in C#:

1. Split file into lines
2. For each line, split by `*` to get segment type and fields (1-indexed per spec, 0-indexed in code)
3. Extract header fields from first occurrence of each segment type:
   - `ID1[0]` → MachineSerialNumber
   - `ID5[0]` → date string (YYYYMMDD), `ID5[1]` → time string (HHMM) → combine into `DateTime`
   - `VA1[0]` → ValueOfPaidVends (int, cents)
4. For each `PA1` segment, begin a new lane record:
   - `PA1[0]` → ProductIdentifier
   - `PA1[1]` → Price (int, cents)
5. For the `PA2` segment immediately following the current `PA1`:
   - `PA2[0]` → NumberOfVends (int)
   - `PA2[1]` → ValueOfPaidSales (int, cents)
6. Collect all lanes, then insert DEXMeter first (get returned Id), then bulk insert DEXLaneMeter rows

---

## 6. Assumptions and Clarifications

- **Monetary values** are stored in cents (integers) as transmitted in the DEX file. No currency conversion is performed.
- **DEXDateTime uniqueness** is per machine — the spec says it will be unique for each machine. A `UNIQUE` constraint on `(Machine, DEXDateTime)` prevents duplicate imports.
- **Machine parameter** comes from the API request, not the DEX file itself. The DEX file does not contain an A/B designation.
- **Basic Auth** is validated on every request. Credentials are read from `appsettings.json` under a `BasicAuth` section.
- **No Users table** is required — credentials are static and config-driven.
- **Concurrent requests** support comes from async/await patterns in the API and connection pooling in SQL.
- **PA segments only** — all other segments (BA, CA, DA, EA, TA, MA, etc.) are ignored during parsing.
- **PA5 date** (last sale date per lane) is not stored in the database per the requirements; only ProductIdentifier, Price, NumberOfVends, and ValueOfPaidSales are stored.
- **Repeated imports** of the same DEX file: behavior on duplicate (Machine, DEXDateTime) should either upsert or reject with a meaningful error. To be decided at implementation time.
- The PDF mentioned in the challenge document is the NAMA DEX specification; it was not provided as a separate file — the two sample .txt files and the .docx suffice for this implementation.

---

## 7. Coding Conventions

These rules apply to **every C# file** in the project without exception.

### Naming
- **PascalCase** for classes, interfaces, methods, and properties
- **camelCase** for local variables and private fields; prefix private fields with `_` (e.g. `_repository`, `_logger`)
- **Interfaces** are prefixed with `I` (e.g. `IDexParser`, `IDexRepository`)
- **No magic strings** — use named constants or values from configuration/`appsettings.json`

### Comments
- **XML doc comments (`///`)** on all public classes and methods
- **Inline comments** only for non-obvious logic (the *why*, not the *what*)
- No commented-out code left in committed files

### General
- No unused `using` statements
- All async methods must use the **`Async` suffix** (e.g. `SaveDexMeterAsync`)
- Use **`var`** only when the type is unambiguous from the right-hand side
- All project files must use **file-scoped namespaces** (`namespace Foo.Bar;` not `namespace Foo.Bar { }`)

### Example skeleton

```csharp
namespace VenDex.Api.Parsing;

/// <summary>
/// Parses raw DEX file text into structured data models.
/// </summary>
public sealed class DexParser : IDexParser
{
    private readonly ILogger<DexParser> _logger;

    public DexParser(ILogger<DexParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parses a DEX file string and returns a <see cref="DexDocument"/>.
    /// </summary>
    public DexDocument Parse(string dexText)
    {
        // ...
    }
}
```
