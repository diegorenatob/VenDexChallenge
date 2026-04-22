# VenDex Challenge — Testing Strategy

## 1. Scope

This document covers unit tests only. Integration tests, end-to-end tests, SQL Server integration, and UI automation are explicitly out of scope for this project.

---

## 2. Test Projects

| Project | Type | Framework | Covers |
|---------|------|-----------|--------|
| `VenDex.Tests` | NUnit unit tests | `net9.0` | Backend: parser, auth, repository |
| `VenDex.Maui.Tests` | NUnit unit tests | `net9.0` | MAUI: ViewModel, ApiService |

---

## 3. Shared Test Packages

```xml
<PackageReference Include="NUnit"                          Version="4.*" />
<PackageReference Include="NUnit3TestAdapter"              Version="4.*" />
<PackageReference Include="Microsoft.NET.Test.Sdk"         Version="17.*" />
<PackageReference Include="NSubstitute"                    Version="5.*" />
```

`VenDex.Tests` additionally references:
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.*" />
```

---

## 4. Backend Tests — VenDex.Tests

### 4.1 DexParserServiceTests

**Location:** `tests/VenDex.Tests/Parsing/DexParserServiceTests.cs`  
**Class under test:** `DexParserService` (implements `IDexParserService`)  
**Dependencies:** none — pure string parsing, no mocks needed  
**Fixture setup:** Load the raw text of `DEX Machine A.txt` and `DEX Machine B.txt` as test string constants (copied from `/Docs`)

#### Test Cases

**Group: Valid DEX — Machine A**

| Test | Input | Expected |
|------|-------|----------|
| `ParseAsync_MachineA_ReturnsMachineSerialNumber` | Machine A DEX text | `DexMeterDto.MachineSerialNumber == "100077238"` |
| `ParseAsync_MachineA_ReturnsDexDateTime` | Machine A DEX text | `DexMeterDto.DexDateTime == new DateTime(2023, 12, 10, 23, 10, 0)` |
| `ParseAsync_MachineA_ReturnsValueOfPaidVends` | Machine A DEX text | `DexMeterDto.ValueOfPaidVends == 344.50m` |
| `ParseAsync_MachineA_ReturnsCorrectLaneCount` | Machine A DEX text | `DexDocument.Lanes.Count == 32` (count of PA1 segments in file) |
| `ParseAsync_MachineA_Lane101_ReturnsPrice` | Machine A DEX text | Lane with `ProductIdentifier == "101"` has `Price == 3.25m` |
| `ParseAsync_MachineA_Lane101_ReturnsNumberOfVends` | Machine A DEX text | Lane `101` has `NumberOfVends == 4` |
| `ParseAsync_MachineA_Lane101_ReturnsValueOfPaidSales` | Machine A DEX text | Lane `101` has `ValueOfPaidSales == 13.00m` |

**Group: Valid DEX — Machine B**

| Test | Input | Expected |
|------|-------|----------|
| `ParseAsync_MachineB_ReturnsMachineSerialNumber` | Machine B DEX text | `DexMeterDto.MachineSerialNumber == "302029479"` |
| `ParseAsync_MachineB_ReturnsDexDateTime` | Machine B DEX text | `DexMeterDto.DexDateTime == new DateTime(2023, 12, 10, 23, 11, 0)` |
| `ParseAsync_MachineB_ReturnsValueOfPaidVends` | Machine B DEX text | `DexMeterDto.ValueOfPaidVends == 4758.85m` |

**Group: Missing Segments**

| Test | Input | Expected |
|------|-------|----------|
| `ParseAsync_MissingID1Segment_ThrowsInvalidOperationException` | DEX text with ID1 line removed | `InvalidOperationException` with message containing `"ID1"` |
| `ParseAsync_MissingVA1Segment_ThrowsInvalidOperationException` | DEX text with VA1 line removed | `InvalidOperationException` with message containing `"VA1"` |
| `ParseAsync_MissingID5Segment_ThrowsInvalidOperationException` | DEX text with ID5 line removed | `InvalidOperationException` with message containing `"ID5"` |

**Group: Malformed Values**

| Test | Input | Expected |
|------|-------|----------|
| `ParseAsync_EmptyBody_ThrowsArgumentException` | `""` | `ArgumentException` |
| `ParseAsync_WhitespaceBody_ThrowsArgumentException` | `"   "` | `ArgumentException` |
| `ParseAsync_MalformedID5Date_ThrowsFormatException` | ID5 with `"BADDATE"` as date field | `FormatException` or `InvalidOperationException` wrapping parse failure |
| `ParseAsync_PA2WithNonNumericVends_ThrowsFormatException` | PA2 line with `"abc"` in field 1 | `FormatException` or `InvalidOperationException` |

---

### 4.2 BasicAuthHandlerTests

**Location:** `tests/VenDex.Tests/Auth/BasicAuthHandlerTests.cs`  
**Class under test:** `BasicAuthHandler`  
**Strategy:** Use `WebApplicationFactory<Program>` (or `TestServer`) to send real HTTP requests through the full middleware pipeline. The test host overrides `appsettings.json` with known credentials using `IConfiguration` in-memory provider.

**Shared setup:**

```
TestServer configured with:
  - BasicAuth:Username = "testuser"
  - BasicAuth:Password = "testpass"
  - Minimal endpoint: GET /ping → 200 OK (to isolate auth from business logic)
```

#### Test Cases

| Test | Authorization Header | Expected Status | Expected Header |
|------|---------------------|-----------------|-----------------|
| `ValidCredentials_Returns200` | `Basic dGVzdHVzZXI6dGVzdHBhc3M=` | 200 | — |
| `WrongPassword_Returns401` | `Basic dGVzdHVzZXI6d3Jvbmc=` (user:wrong) | 401 | `WWW-Authenticate: Basic realm="VenDex"` |
| `WrongUsername_Returns401` | `Basic d3Jvbmc6dGVzdHBhc3M=` (wrong:pass) | 401 | `WWW-Authenticate: Basic realm="VenDex"` |
| `MissingAuthHeader_Returns401` | *(no header)* | 401 | `WWW-Authenticate: Basic realm="VenDex"` |
| `MalformedBase64_Returns401` | `Basic not-valid-base64!!!` | 401 | `WWW-Authenticate: Basic realm="VenDex"` |
| `NonBasicScheme_Returns401` | `Bearer sometoken` | 401 | `WWW-Authenticate: Basic realm="VenDex"` |
| `CredentialsMissingColon_Returns401` | `Basic dGVzdHVzZXI=` (no colon in decoded value) | 401 | `WWW-Authenticate: Basic realm="VenDex"` |

---

### 4.3 DexRepositoryTests

**Location:** `tests/VenDex.Tests/Repository/DexRepositoryTests.cs`  
**Class under test:** `DexRepository`  
**Strategy:** NSubstitute mock of `VenDexDbContext` (specifically its `Database` property and `ExecuteSqlRawAsync` call). Verifies that the correct SP name and parameter values are passed, and that the `OUTPUT` parameter value is read back correctly.

**Important:** `Database.ExecuteSqlRawAsync` is an extension method on `DatabaseFacade`. To make it mockable, `DexRepository` should accept a `VenDexDbContext` injected via DI; the test constructs the context with the InMemory provider to get a valid `DatabaseFacade` instance, then intercepts via a test-double DbContext subclass or wraps the call behind a thin interface.

#### Test Cases

**SaveDexMeterAsync**

| Test | Setup | Verifies |
|------|-------|----------|
| `SaveDexMeterAsync_ExecutesCorrectStoredProcedureName` | Mock `ExecuteSqlRawAsync` intercept | SQL string contains `"SaveDEXMeter"` |
| `SaveDexMeterAsync_PassesMachineParameter` | dto.Machine = `"A"` | `@Machine` SqlParameter value = `"A"` |
| `SaveDexMeterAsync_PassesDexDateTimeParameter` | dto.DexDateTime = known DateTime | `@DEXDateTime` value matches |
| `SaveDexMeterAsync_PassesMachineSerialNumberParameter` | dto.MachineSerialNumber = `"100077238"` | `@MachineSerialNumber` value matches |
| `SaveDexMeterAsync_PassesValueOfPaidVendsParameter` | dto.ValueOfPaidVends = `344.50m` | `@ValueOfPaidVends` value matches |
| `SaveDexMeterAsync_ReadsOutputParameterAsReturnValue` | OUTPUT param set to `42` by mock | Method returns `42` |

**SaveDexLaneMeterAsync**

| Test | Setup | Verifies |
|------|-------|----------|
| `SaveDexLaneMeterAsync_ExecutesCorrectStoredProcedureName` | Mock intercept | SQL string contains `"SaveDEXLaneMeter"` |
| `SaveDexLaneMeterAsync_PassesDexMeterIdParameter` | dexMeterId = `42` | `@DexMeterId` value = `42` |
| `SaveDexLaneMeterAsync_PassesProductIdentifierParameter` | dto.ProductIdentifier = `"101"` | `@ProductIdentifier` value matches |
| `SaveDexLaneMeterAsync_PassesPriceParameter` | dto.Price = `3.25m` | `@Price` value matches |
| `SaveDexLaneMeterAsync_PassesNumberOfVendsParameter` | dto.NumberOfVends = `4` | `@NumberOfVends` value matches |
| `SaveDexLaneMeterAsync_PassesValueOfPaidSalesParameter` | dto.ValueOfPaidSales = `13.00m` | `@ValueOfPaidSales` value matches |

---

## 5. MAUI Tests — VenDex.Maui.Tests

### 5.1 MainViewModelTests

**Location:** `tests/VenDex.Maui.Tests/ViewModels/MainViewModelTests.cs`  
**Class under test:** `MainViewModel`  
**Dependencies mocked with NSubstitute:** `IApiService`, `IDexFileService`

**Shared setup:**

```
IDexFileService stub:
  .LoadDexFile("A") returns "DEX_A_CONTENT"
  .LoadDexFile("B") returns "DEX_B_CONTENT"
```

#### Test Cases

**IsBusy state**

| Test | Scenario | Verifies |
|------|----------|----------|
| `SendDexACommand_WhenExecuted_SetsIsBusyTrue` | ApiService delays; check state mid-execution | `IsBusy == true` during async call |
| `SendDexACommand_OnSuccess_ClearsIsBusy` | ApiService returns `ApiResult.Success()` | `IsBusy == false` after await |
| `SendDexACommand_OnFailure_ClearsIsBusy` | ApiService returns `ApiResult.Failure("err")` | `IsBusy == false` after await |

**CanExecute / button disabling**

| Test | Scenario | Verifies |
|------|----------|----------|
| `SendDexBCommand_WhileAIsRunning_CannotExecute` | A command in flight | `SendDexBCommand.CanExecute() == false` |
| `SendDexACommand_WhileBIsRunning_CannotExecute` | B command in flight | `SendDexACommand.CanExecute() == false` |
| `BothCommands_AfterCompletion_CanExecute` | Command finishes | Both `.CanExecute() == true` |

**Success path**

| Test | Scenario | Verifies |
|------|----------|----------|
| `SendDexACommand_OnSuccess_SetsSuccessStatusMessage` | ApiService returns Success | `StatusMessage` is non-empty and contains `"A"` |
| `SendDexACommand_OnSuccess_IsErrorFalse` | ApiService returns Success | `IsError == false` |
| `SendDexBCommand_OnSuccess_SetsStatusMessageContainingB` | ApiService returns Success | `StatusMessage` contains `"B"` |

**Error path**

| Test | Scenario | Verifies |
|------|----------|----------|
| `SendDexACommand_OnFailure_SetsErrorMessage` | ApiService returns `ApiResult.Failure("HTTP 500")` | `StatusMessage == "HTTP 500"` |
| `SendDexACommand_OnFailure_IsErrorTrue` | ApiService returns Failure | `IsError == true` |
| `SendDexACommand_OnFailure_RaisesOnSendFailedEvent` | ApiService returns Failure | `OnSendFailed` event raised with error message |

**DexFileService interaction**

| Test | Scenario | Verifies |
|------|----------|----------|
| `SendDexACommand_LoadsDexFileForMachineA` | Command executes | `IDexFileService.LoadDexFile("A")` called once |
| `SendDexBCommand_LoadsDexFileForMachineB` | Command executes | `IDexFileService.LoadDexFile("B")` called once |
| `SendDexACommand_SendsLoadedContentToApiService` | `LoadDexFile` returns `"DEX_A_CONTENT"` | `IApiService.SendDexFileAsync("A", "DEX_A_CONTENT")` called |

---

### 5.2 ApiServiceTests

**Location:** `tests/VenDex.Maui.Tests/Services/ApiServiceTests.cs`  
**Class under test:** `ApiService`  
**Strategy:** Inject a `MockHttpMessageHandler` (custom `DelegatingHandler`) into `HttpClient` via `IHttpClientFactory` stub. Captures the outgoing `HttpRequestMessage` and returns a configured `HttpResponseMessage`.

**Shared setup:**

```
MockHttpMessageHandler captures:
  - Last HttpRequestMessage sent
  - Returns configurable HttpResponseMessage
```

#### Test Cases

**Authorization header**

| Test | Setup | Verifies |
|------|-------|----------|
| `SendDexFileAsync_SetsBasicAuthorizationHeader` | Any call | `Authorization` header present |
| `SendDexFileAsync_AuthorizationHeaderSchemeIsBasic` | Any call | Scheme == `"Basic"` |
| `SendDexFileAsync_AuthorizationHeaderDecodesCorrectly` | Any call | Base64 decode == `"vendsys:NFsZGmHAGWJSZ#RuvdiV"` |

**Request shape**

| Test | Setup | Verifies |
|------|-------|----------|
| `SendDexFileAsync_PostsToCorrectPath` | machine = `"A"` | Request URI path == `/vdi-dex` |
| `SendDexFileAsync_IncludesMachineQueryParam` | machine = `"A"` | Query string contains `machine=A` |
| `SendDexFileAsync_UsesTextPlainContentType` | Any call | `Content-Type == "text/plain"` |
| `SendDexFileAsync_BodyMatchesDexContent` | dexContent = `"DEXDATA"` | Request body == `"DEXDATA"` |
| `SendDexFileAsync_UsesPostMethod` | Any call | `HttpMethod == POST` |

**Response handling**

| Test | Handler returns | Verifies |
|------|-----------------|----------|
| `SendDexFileAsync_On200_ReturnsSuccess` | 200 OK | `ApiResult.IsSuccess == true` |
| `SendDexFileAsync_On401_ReturnsFailure` | 401 Unauthorized | `IsSuccess == false`, message contains `"401"` |
| `SendDexFileAsync_On500_ReturnsFailure` | 500 Internal Server Error | `IsSuccess == false`, message contains `"500"` |
| `SendDexFileAsync_OnNetworkException_ReturnsFailure` | `HttpRequestException` thrown | `IsSuccess == false`, `ErrorMessage` non-null |

**Polly retry**

| Test | Setup | Verifies |
|------|-------|----------|
| `SendDexFileAsync_On503_RetriesThreeTimes` | Handler returns 503 first 3× then 200 | Handler called 4× total; final `IsSuccess == true` |
| `SendDexFileAsync_OnPersistent503_ReturnsFailure` | Handler always returns 503 | Handler called 4× total; final `IsSuccess == false` |
| `SendDexFileAsync_On401_DoesNotRetry` | Handler returns 401 | Handler called exactly 1× (not retried) |

---

## 6. Out of Scope

| Category | Reason |
|----------|--------|
| Integration tests (API + real SQL Server) | Requires a live LocalDB instance — environment-dependent, not reliable in CI without extra setup |
| EF Core migration tests | Migration correctness is verified by running `dotnet ef database update` manually during development |
| UI automation (Appium, MAUI UI Testing) | Requires a running emulator/device; not feasible within the time-boxed project scope |
| SQL Server stored procedure tests | SP correctness is verified manually via SSMS against the LocalDB after migration runs |
| End-to-end (MAUI → API → DB) | Covered by manual testing with the running application |

---

## 7. Running Tests

```bash
# Backend tests
dotnet test tests/VenDex.Tests/VenDex.Tests.csproj

# MAUI tests
dotnet test tests/VenDex.Maui.Tests/VenDex.Maui.Tests.csproj

# All tests
dotnet test VenDexChallenge.sln
```
