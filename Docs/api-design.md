# VendSys Challenge — API Design

## 1. Overview

**Framework:** ASP.NET Core 9 Minimal API  
**Project:** `VendSys.Api`  
**Endpoint count:** 1  
**Auth scheme:** HTTP Basic Authentication

---

## 2. Endpoint

### POST /vdi-dex

Receives a raw DEX file, parses it, and persists the extracted data via stored procedures.

---

## 3. Request Specification

### 3.1 HTTP Method and Path

```
POST /vdi-dex
```

### 3.2 Query Parameters

| Parameter | Type | Required | Values | Description |
|-----------|------|----------|--------|-------------|
| `machine` | string | Yes | `"A"` or `"B"` | Identifies which machine the DEX file belongs to. The DEX file itself contains no A/B label. |

### 3.3 Headers

| Header | Required | Value |
|--------|----------|-------|
| `Authorization` | Yes | `Basic <base64(vendsys:NFsZGmHAGWJSZ#RuvdiV)>` |
| `Content-Type` | Yes | `text/plain` |

### 3.4 Request Body

Raw DEX file text. Line-delimited, fields delimited by `*`. No JSON wrapper.

**Example:**
```
DXS*STF0000000*VA*V0/6*1
ST*001*0001
ID1*100077238*187**Location Not Set**MerchantG*6*1
...
PA1*101*325*101****0**
PA2*4*1300*0*0*0*0*0*0*0*0*0*0
...
DXE*1*1
```

---

## 4. Response Specification

### 4.1 200 OK — Success

```json
{
  "machine": "A",
  "serialNumber": "100077238",
  "dexDateTime": "2023-12-10T23:10:00",
  "valueOfPaidVends": 344.50,
  "lanesProcessed": 32
}
```

| Field | Type | Description |
|-------|------|-------------|
| `machine` | `string` | Echo of the `machine` query param |
| `serialNumber` | `string` | `MachineSerialNumber` extracted from ID1 |
| `dexDateTime` | `string` (ISO 8601) | Timestamp extracted from ID5 |
| `valueOfPaidVends` | `number` | Total value in dollars (cents ÷ 100) |
| `lanesProcessed` | `number` | Count of PA segment pairs saved |

### 4.2 401 Unauthorized

Returned when the `Authorization` header is absent, malformed, or the decoded credentials do not match `appsettings.json`.

```
HTTP/1.1 401 Unauthorized
WWW-Authenticate: Basic realm="VendSys"
```

Body: empty (standard HTTP 401 — no JSON, to avoid leaking auth detail).

### 4.3 400 Bad Request

Returned for recoverable client errors: missing `machine` param, invalid `machine` value, or a DEX body that fails parsing (missing required segments).

```json
{
  "error": "Query parameter 'machine' is required and must be 'A' or 'B'."
}
```

```json
{
  "error": "DEX file is missing required segment 'VA1'."
}
```

### 4.4 500 Internal Server Error

Returned for unexpected failures (database connectivity, unhandled exceptions). Emitted by `GlobalExceptionMiddleware`.

```json
{
  "error": "An unexpected error occurred.",
  "traceId": "00-a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6-e7f8a9b0c1d2e3f4-00"
}
```

`traceId` is the W3C Trace Context identifier from `HttpContext.TraceIdentifier`, included to correlate with Serilog log entries.

---

## 5. Middleware Pipeline

Registered in `Program.cs` in the following order. Outermost middleware is registered first.

```
Incoming request
      │
      ▼
┌─────────────────────────────┐
│  Serilog Request Logging    │  ← 1. Outermost: logs every request/response
│  (UseSerilogRequestLogging) │     including 401s and 500s
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐
│  Global Exception Handler   │  ← 2. Catches any unhandled exception from
│  (GlobalExceptionMiddleware)│     auth or the endpoint; writes 500 JSON body
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐
│  UseAuthentication          │  ← 3. Triggers BasicAuthHandler; sets
│  (BasicAuthHandler)         │     HttpContext.User on success, 401 on failure
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐
│  UseAuthorization           │  ← 4. Enforces [Authorize] on the endpoint;
│                             │     returns 403 if auth succeeded but policy fails
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐
│  POST /vdi-dex              │  ← 5. Endpoint handler: validate params,
│  (DexEndpoints)             │     parse DEX, call use case, return 200
└─────────────────────────────┘
```

**Why Serilog is outermost:** placing it before the error handler means every request — including ones that fail authentication or crash — is logged with its final status code and duration.

**Why GlobalExceptionMiddleware is second:** it wraps the auth and endpoint layers so any uncaught exception (including from `BasicAuthHandler` itself) is rendered as a consistent JSON 500 rather than a raw ASP.NET error page.

---

## 6. Request Processing Flow

```
POST /vdi-dex?machine=A
  │
  ├─ [BasicAuthHandler]
  │     Decode base64 from Authorization header
  │     Compare username/password against appsettings.json BasicAuth section
  │     → 401 if mismatch
  │
  ├─ DexEndpoints.HandleAsync(machine, body)
  │
  ├─ Validate machine param
  │     machine must be "A" or "B"
  │     → 400 Bad Request if invalid or missing
  │
  ├─ Validate body is non-empty
  │     → 400 Bad Request if body is null or whitespace
  │
  ├─ ProcessDexFileUseCase.ExecuteAsync(dexText, machine)
  │
  │   ├─ IDexParserService.ParseAsync(dexText)
  │   │     → DexDocument { DexMeterDto, List<DexLaneMeterDto> }
  │   │     → throws InvalidOperationException if required segment missing
  │   │       (caught by endpoint, mapped to 400)
  │   │
  │   ├─ IDexRepository.SaveDexMeterAsync(DexMeterDto)
  │   │     → EXEC SaveDEXMeter ... OUTPUT @DexMeterId
  │   │     → returns int dexMeterId
  │   │
  │   └─ foreach lane in DexDocument.Lanes
  │         IDexRepository.SaveDexLaneMeterAsync(dexMeterId, DexLaneMeterDto)
  │         → EXEC SaveDEXLaneMeter ...
  │
  └─ 200 OK
       { machine, serialNumber, dexDateTime, valueOfPaidVends, lanesProcessed }
```

---

## 7. HTTP Basic Authentication

### 7.1 Scheme Registration

`BasicAuthHandler` is registered as a named authentication scheme in `Program.cs`:

```csharp
builder.Services
    .AddAuthentication("Basic")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>("Basic", null);

builder.Services.AddAuthorization();
```

The endpoint is decorated with `[Authorize]` (or `.RequireAuthorization()` in Minimal API style).

### 7.2 BasicAuthHandler Validation Flow

```
1. Read Authorization header
   Missing → AuthenticateResult.NoResult() → 401

2. Verify scheme is "Basic"
   Other scheme → AuthenticateResult.NoResult() → 401

3. Base64 decode the credential string
   Malformed → AuthenticateResult.Fail("Invalid Authorization header") → 401

4. Split on first ':' → username, password
   No ':' present → AuthenticateResult.Fail("Invalid credential format") → 401

5. Compare against appsettings.json:
     BasicAuth:Username  = "vendsys"
     BasicAuth:Password  = "NFsZGmHAGWJSZ#RuvdiV"
   Mismatch → AuthenticateResult.Fail("Invalid credentials") → 401

6. Match → AuthenticateResult.Success(ticket)
   ClaimsPrincipal constructed with Name = username
```

### 7.3 Challenge Response

`BasicAuthHandler.ChallengeAsync` adds the `WWW-Authenticate` header:

```
WWW-Authenticate: Basic realm="VendSys"
```

### 7.4 Credentials in appsettings.json

```json
"BasicAuth": {
  "Username": "vendsys",
  "Password": "NFsZGmHAGWJSZ#RuvdiV"
}
```

`BasicAuthHandler` receives `IConfiguration` (or a typed `BasicAuthOptions` bound in DI) and reads these values. No hardcoded credential strings appear in C# source.

---

## 8. Global Exception Middleware

`GlobalExceptionMiddleware` wraps `next(context)` in a try/catch:

```
try
{
    await next(context);
}
catch (InvalidOperationException ex)   // DEX parse failures
{
    context.Response.StatusCode  = 400;
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(new { error = ex.Message });
}
catch (Exception)
{
    context.Response.StatusCode  = 500;
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(new
    {
        error   = "An unexpected error occurred.",
        traceId = Activity.Current?.Id ?? context.TraceIdentifier
    });
}
```

**Separation of concerns:** `InvalidOperationException` is the agreed contract for known parse errors thrown by `IDexParserService`. All other exceptions fall through to the generic 500 handler.

---

## 9. Serilog Request Logging

Configured in `Program.cs` with enrichers that add machine-specific context:

```csharp
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("Machine",    httpContext.Request.Query["machine"].ToString());
        diagnosticContext.Set("RemoteIP",   httpContext.Connection.RemoteIpAddress?.ToString());
        diagnosticContext.Set("RequestId",  httpContext.TraceIdentifier);
    };
});
```

**Log output format (console):**
```
[2023-12-10 23:10:42.381 +00:00] INF HTTP POST /vdi-dex responded 200 in 87.4 ms
  {Machine: "A", RemoteIP: "::1", RequestId: "..."}
```

**Rolling file:** `logs/api-20231210.log` — one file per day, retained for 14 days.

---

## 10. Concurrency

- All I/O operations are `async`/`await` throughout (no blocking `.Result` or `.Wait()`).
- `VendSysDbContext` is registered with `AddDbContext<>` (scoped lifetime) — each request receives its own context instance from the DI scope.
- `ProcessDexFileUseCase` and its dependencies are scoped or transient — no shared mutable state.
- SQL Server handles concurrent stored procedure calls naturally via row-level locking.

---

## 11. DI Registration Summary (VendSys.Api — Program.cs)

```csharp
// Auth
builder.Services
    .AddAuthentication("Basic")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>("Basic", null);
builder.Services.AddAuthorization();

// Options
builder.Services.Configure<BasicAuthOptions>(
    builder.Configuration.GetSection("BasicAuth"));

// Database
builder.Services.AddDbContext<VendSysDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Application layer
builder.Services.AddScoped<IDexParserService, DexParserService>();
builder.Services.AddScoped<IDexRepository, DexRepository>();
builder.Services.AddScoped<ProcessDexFileUseCase>();

// Middleware
builder.Services.AddTransient<GlobalExceptionMiddleware>();
```
