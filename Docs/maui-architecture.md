# VendSys Challenge — MAUI Application Architecture

## 1. Project Overview

**Project name:** `VendSys.Maui`  
**Target framework:** `net9.0-windows10.0.19041.0;net9.0-android;net9.0-ios`  
**Pattern:** MVVM (Model–View–ViewModel)  
**DI container:** `Microsoft.Extensions.DependencyInjection` (built into MAUI host)  
**Community library:** `CommunityToolkit.Mvvm` (IAsyncRelayCommand, ObservableObject)  
**HTTP resilience:** `Microsoft.Extensions.Http.Polly` + `Polly`

---

## 2. Full Folder and File Structure

```
src/VendSys.Maui/
│
├── Constants/
│   └── ApiConstants.cs              ← base URL, endpoint path, machine identifiers
│
├── Models/
│   ├── DexMeter.cs                  ← machine-level data (mirrors Domain entity)
│   └── DexLaneMeter.cs             ← lane-level data (mirrors Domain entity)
│
├── Services/
│   ├── IApiService.cs               ← contract for HTTP calls
│   ├── ApiService.cs                ← implementation using IHttpClientFactory
│   ├── IDexFileService.cs           ← contract for loading embedded DEX files
│   └── DexFileService.cs           ← loads files via Assembly.GetManifestResourceStream
│
├── ViewModels/
│   └── MainViewModel.cs            ← ObservableObject, IsBusy, SendDexACommand, SendDexBCommand
│
├── Views/
│   └── MainPage.xaml               ← two buttons, status label, activity indicator
│   └── MainPage.xaml.cs
│
├── Resources/
│   ├── AppIcon/                     ← MAUI default app icon
│   ├── Fonts/                       ← MAUI default fonts
│   ├── Images/                      ← MAUI default images
│   ├── Raw/
│   │   ├── MachineA.txt            ← EmbeddedResource: DEX Machine A sample
│   │   └── MachineB.txt            ← EmbeddedResource: DEX Machine B sample
│   └── Styles/
│       └── Colors.xaml             ← named color resource dictionary
│
├── App.xaml                         ← merged resource dictionaries
├── App.xaml.cs
├── AppShell.xaml
├── AppShell.xaml.cs
├── MauiProgram.cs                   ← composition root / DI registration
└── VendSys.Maui.csproj
```

### 2.1 Test Project

```
tests/VendSys.Maui.Tests/
├── ViewModels/
│   └── MainViewModelTests.cs       ← command behaviour, IsBusy state, error handling
├── Services/
│   └── ApiServiceTests.cs          ← mocked HttpMessageHandler, auth header, Polly retry
└── VendSys.Maui.Tests.csproj
```

---

## 3. Project File Configuration (VendSys.Maui.csproj highlights)

```xml
<ItemGroup>
  <!-- DEX files compiled into the assembly as embedded resources -->
  <EmbeddedResource Include="Resources\Raw\MachineA.txt">
    <LogicalName>VendSys.Maui.Resources.Raw.MachineA.txt</LogicalName>
  </EmbeddedResource>
  <EmbeddedResource Include="Resources\Raw\MachineB.txt">
    <LogicalName>VendSys.Maui.Resources.Raw.MachineB.txt</LogicalName>
  </EmbeddedResource>
</ItemGroup>

<ItemGroup>
  <PackageReference Include="CommunityToolkit.Mvvm" Version="8.*" />
  <PackageReference Include="CommunityToolkit.Maui" Version="9.*" />
  <PackageReference Include="Microsoft.Extensions.Http.Polly" Version="9.*" />
  <PackageReference Include="Polly" Version="8.*" />
</ItemGroup>
```

**Logical name convention:** `<AssemblyName>.<folder>.<folder>.<filename>` — the `LogicalName` must match exactly what is passed to `GetManifestResourceStream`.

---

## 4. Constants

### ApiConstants.cs

All API-related strings live here as `public const string`. No string literals appear anywhere else in the codebase.

```
namespace VendSys.Maui.Constants;

public static class ApiConstants
{
    public const string HttpClientName  = "VendSysApiClient";
    public const string BaseUrl         = "http://10.0.2.2:8080";     // Android emulator loopback
    public const string DexEndpoint     = "/vdi-dex";
    public const string MachineParamKey = "machine";
    public const string MachineA        = "A";
    public const string MachineB        = "B";
    public const string AuthUsername    = "vendsys";
    public const string AuthPassword    = "NFsZGmHAGWJSZ#RuvdiV";
    public const string AuthScheme      = "Basic";
}
```

**Note on BaseUrl:** `10.0.2.2` is the Android emulator's alias for the host machine's localhost. For iOS simulator it is `localhost`. For a real device on the same LAN, it is the host's LAN IP. This can be overridden at build time via a `#if` preprocessor or build configuration — document this in the project README.

### Resource Keys (used in XAML, no magic strings)

```
ResourceKeys.cs (or defined directly in Colors.xaml)
  PrimaryColor    → brand colour
  SurfaceColor    → page background
  OnSurfaceColor  → text on surface
  ErrorColor      → error state label/icon
  BusyColor       → activity indicator tint
```

---

## 5. DI Registration — MauiProgram.cs

```
MauiApp.CreateBuilder(args)
  .UseMauiApp<App>()
  .UseMauiCommunityToolkit()
  .ConfigureFonts(...)
  │
  ├── services.AddSingleton<IDexFileService, DexFileService>()
  │     DexFileService has no mutable state; safe as singleton.
  │
  ├── services.AddTransient<IApiService, ApiService>()
  │     ApiService receives IHttpClientFactory; transient is fine.
  │
  ├── services.AddTransient<MainViewModel>()
  │     New VM per navigation to MainPage.
  │
  ├── services.AddTransient<MainPage>()
  │     MAUI requires views to be registered for Shell navigation.
  │
  └── services.AddHttpClient(ApiConstants.HttpClientName, client =>
          {
              client.BaseAddress = new Uri(ApiConstants.BaseUrl);
              // Authorization header set per-request in ApiService,
              // not here, to keep the factory config stateless.
          })
          .AddPolicyHandler(GetRetryPolicy())
```

### Polly Retry Policy (defined as a static helper in MauiProgram.cs)

```
private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    => HttpPolicyExtensions
           .HandleTransientHttpError()           // 5xx + HttpRequestException
           .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
           .WaitAndRetryAsync(
               retryCount: 3,
               sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
               // 1st retry: 2s, 2nd: 4s, 3rd: 8s
               onRetry: (outcome, timespan, attempt, _) =>
                   Debug.WriteLine($"Retry {attempt} after {timespan.TotalSeconds}s: {outcome.Exception?.Message}")
           );
```

**Why `HandleTransientHttpError`:** covers `HttpRequestException` (network unreachable), 408 Request Timeout, and 5xx server errors. It does **not** retry 401 Unauthorized (correct — that is an auth configuration error, not transient).

---

## 6. Services

### 6.1 IDexFileService / DexFileService

**Contract:**
```
public interface IDexFileService
{
    /// <summary>Loads the full text of an embedded DEX file by machine identifier.</summary>
    string LoadDexFile(string machine);
}
```

**Implementation — how embedded resources are loaded:**

```
Assembly  assembly    = Assembly.GetExecutingAssembly();
string    resourceKey = machine == ApiConstants.MachineA
                          ? "VendSys.Maui.Resources.Raw.MachineA.txt"
                          : "VendSys.Maui.Resources.Raw.MachineB.txt";

using Stream stream = assembly.GetManifestResourceStream(resourceKey)
    ?? throw new InvalidOperationException($"Embedded resource '{resourceKey}' not found.");

using StreamReader reader = new(stream);
return reader.ReadToEnd();
```

`GetManifestResourceStream` returns `null` if the logical name does not match exactly — the `??` guard surfaces this as a clear exception rather than a null reference downstream.

**Why `string` not `Task<string>`:** Embedded resource loading is synchronous (in-memory stream from the assembly); wrapping in `Task` would add overhead with no benefit.

### 6.2 IApiService / ApiService

**Contract:**
```
public interface IApiService
{
    /// <summary>Posts a DEX file to the API endpoint for the given machine.</summary>
    Task<ApiResult> SendDexFileAsync(string machine, string dexContent);
}
```

**ApiResult (simple discriminated union):**
```
public sealed record ApiResult(bool IsSuccess, string? ErrorMessage = null)
{
    public static ApiResult Success()               => new(true);
    public static ApiResult Failure(string reason) => new(false, reason);
}
```

**ApiService implementation flow:**
```
1. Create HttpRequestMessage:
     Method  = POST
     Uri     = ApiConstants.DexEndpoint + "?machine=" + machine
     Content = new StringContent(dexContent, Encoding.UTF8, "text/plain")

2. Add Authorization header (per-request, not on factory):
     string credentials = Convert.ToBase64String(
         Encoding.UTF8.GetBytes($"{ApiConstants.AuthUsername}:{ApiConstants.AuthPassword}"));
     request.Headers.Authorization =
         new AuthenticationHeaderValue(ApiConstants.AuthScheme, credentials);

3. Send via _httpClientFactory.CreateClient(ApiConstants.HttpClientName)
   (Polly retry is attached to the named client pipeline — transparent to ApiService)

4. Inspect response:
     response.IsSuccessStatusCode → ApiResult.Success()
     otherwise                    → ApiResult.Failure($"HTTP {(int)response.StatusCode}")

5. Catch HttpRequestException (network unreachable, all retries exhausted):
     → ApiResult.Failure("Network error: " + ex.Message)
```

---

## 7. ViewModel — MainViewModel

### 7.1 Properties

| Property | Type | Purpose |
|----------|------|---------|
| `IsBusy` | `bool` | True while an HTTP call is in flight |
| `StatusMessage` | `string` | Displayed below the buttons |
| `IsError` | `bool` | Drives error styling on the status label |
| `SendDexACommand` | `IAsyncRelayCommand` | Triggers Machine A send |
| `SendDexBCommand` | `IAsyncRelayCommand` | Triggers Machine B send |

`MainViewModel` extends `ObservableObject` from `CommunityToolkit.Mvvm`. Properties use `[ObservableProperty]` source-generated backing fields where applicable.

### 7.2 State Machine

```
         ┌─────────────────────────────────┐
         │             IDLE                │
         │  IsBusy = false                 │
         │  StatusMessage = ""             │
         │  IsError = false                │
         │  Both buttons enabled           │
         └──────────┬──────────────────────┘
                    │  User taps button A or B
                    ▼
         ┌─────────────────────────────────┐
         │           LOADING               │
         │  IsBusy = true                  │
         │  StatusMessage = "Sending..."   │
         │  IsError = false                │
         │  Both buttons disabled          │ ← CanExecute = !IsBusy on both commands
         └──────┬──────────────┬───────────┘
                │              │
         Success│         Error│ (HTTP non-2xx or network failure)
                ▼              ▼
  ┌─────────────────┐   ┌──────────────────────────────────────┐
  │     SUCCESS     │   │                ERROR                 │
  │  IsBusy = false │   │  IsBusy = false                      │
  │  StatusMessage  │   │  StatusMessage = ApiResult.ErrorMsg  │
  │  = "Sent ✓"     │   │  IsError = true                      │
  │  IsError = false│   │  Alert dialog shown (MAUI Alerts API) │
  │  Buttons enabled│   │  Buttons re-enabled                  │
  └─────────────────┘   └──────────────────────────────────────┘
         │                            │
         └──────────┬─────────────────┘
                    │  Next button tap
                    ▼
                  IDLE (reset StatusMessage, IsError)
```

### 7.3 IsBusy / CanExecute wiring

`IAsyncRelayCommand` from CommunityToolkit.Mvvm automatically calls `CanExecuteChanged` when the command's internal `IsRunning` changes. Both `SendDexACommand` and `SendDexBCommand` share `IsBusy` as their `CanExecute` predicate:

```
SendDexACommand = new AsyncRelayCommand(
    execute:    () => SendDexFileAsync(ApiConstants.MachineA),
    canExecute: () => !IsBusy);

SendDexBCommand = new AsyncRelayCommand(
    execute:    () => SendDexFileAsync(ApiConstants.MachineB),
    canExecute: () => !IsBusy);
```

When `IsBusy` changes, `NotifyCanExecuteChanged()` is called on **both** commands so that tapping one button also disables the other for the duration of the request.

### 7.4 SendDexFileAsync internal flow

```
private async Task SendDexFileAsync(string machine)
{
    IsBusy        = true;
    IsError       = false;
    StatusMessage = "Sending...";
    SendDexACommand.NotifyCanExecuteChanged();
    SendDexBCommand.NotifyCanExecuteChanged();

    string dexContent = _dexFileService.LoadDexFile(machine);
    ApiResult result  = await _apiService.SendDexFileAsync(machine, dexContent);

    if (result.IsSuccess)
    {
        StatusMessage = $"Machine {machine} sent successfully.";
        IsError       = false;
    }
    else
    {
        StatusMessage = result.ErrorMessage ?? "Unknown error.";
        IsError       = true;
        // Shell.Current.DisplayAlert used from view layer; ViewModel raises event
        OnSendFailed?.Invoke(this, result.ErrorMessage!);
    }

    IsBusy = false;
    SendDexACommand.NotifyCanExecuteChanged();
    SendDexBCommand.NotifyCanExecuteChanged();
}
```

**Error surface strategy:** The ViewModel raises an event (`OnSendFailed`) that the View's code-behind subscribes to and calls `DisplayAlert`. This keeps `Shell`/`Application.Current` references out of the ViewModel, making it fully unit-testable.

---

## 8. View — MainPage.xaml

### 8.1 Layout Description

```
ContentPage
  └── ScrollView
        └── VerticalStackLayout  (Padding="16", Spacing="24")
              │
              ├── Label            ← App title / heading
              │     Text="VendSys DEX Uploader"
              │     Style="{StaticResource HeadlineStyle}"
              │
              ├── Label            ← Subtitle / instructions
              │     Text="Select a machine to send its DEX report to the API."
              │     Style="{StaticResource SubtitleStyle}"
              │
              ├── Button           ← Machine A
              │     Text="Send Machine A"
              │     Command="{Binding SendDexACommand}"
              │     AutomationProperties.Name="Send DEX report for Machine A"
              │     AutomationProperties.HelpText="Posts Machine A DEX data to the API"
              │     Style="{StaticResource PrimaryButtonStyle}"
              │
              ├── Button           ← Machine B
              │     Text="Send Machine B"
              │     Command="{Binding SendDexBCommand}"
              │     AutomationProperties.Name="Send DEX report for Machine B"
              │     AutomationProperties.HelpText="Posts Machine B DEX data to the API"
              │     Style="{StaticResource PrimaryButtonStyle}"
              │
              ├── ActivityIndicator
              │     IsRunning="{Binding IsBusy}"
              │     IsVisible="{Binding IsBusy}"
              │     Color="{StaticResource BusyColor}"
              │
              └── Label            ← Status / error message
                    Text="{Binding StatusMessage}"
                    TextColor="{Binding IsError,
                                Converter={StaticResource BoolToColorConverter},
                                ConverterParameter='ErrorColor|OnSurfaceColor'}"
                    IsVisible="{Binding StatusMessage,
                                Converter={StaticResource StringToBoolConverter}}"
```

### 8.2 Resource Dictionary (App.xaml / Colors.xaml)

No hexadecimal color literals appear in page XAML. All colors are declared once in `Colors.xaml` and referenced by name:

```xml
<!-- Colors.xaml -->
<Color x:Key="PrimaryColor">#0057A8</Color>
<Color x:Key="PrimaryDarkColor">#003F7A</Color>
<Color x:Key="SurfaceColor">#F5F5F5</Color>
<Color x:Key="OnSurfaceColor">#1C1C1E</Color>
<Color x:Key="ErrorColor">#D32F2F</Color>
<Color x:Key="BusyColor">#0057A8</Color>

<!-- Button style -->
<Style x:Key="PrimaryButtonStyle" TargetType="Button">
    <Setter Property="BackgroundColor" Value="{StaticResource PrimaryColor}" />
    <Setter Property="TextColor"       Value="White" />
    <Setter Property="CornerRadius"    Value="8" />
    <Setter Property="FontSize"        Value="16" />
    <Setter Property="HeightRequest"   Value="50" />
    <Setter Property="HorizontalOptions" Value="Fill" />
</Style>
```

`App.xaml` merges `Colors.xaml` and the MAUI default styles:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="Resources/Styles/Colors.xaml" />
            <ResourceDictionary Source="Resources/Styles/Styles.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

### 8.3 Value Converters

| Converter | Input | Output | Use |
|-----------|-------|--------|-----|
| `BoolToColorConverter` | `bool` + param `"ErrorColor\|OnSurfaceColor"` | `Color` | Status label text colour |
| `StringToBoolConverter` | `string` | `bool` (non-null, non-empty) | Status label visibility |

Both converters are registered as static resources in `App.xaml` and referenced in XAML with `{StaticResource}`.

---

## 9. Embedded DEX File Loading — End-to-End

```
Build time:
  Resources/Raw/MachineA.txt  ──→  EmbeddedResource
  Resources/Raw/MachineB.txt  ──→  EmbeddedResource
  (LogicalName set in .csproj so the resource key is predictable)

Runtime (DexFileService.LoadDexFile("A")):
  1. Assembly.GetExecutingAssembly()
  2. GetManifestResourceStream("VendSys.Maui.Resources.Raw.MachineA.txt")
  3. StreamReader.ReadToEnd()  →  returns full DEX string

If resource key mismatch (wrong LogicalName):
  GetManifestResourceStream returns null
  DexFileService throws InvalidOperationException("Embedded resource '...' not found.")
  MainViewModel catches, sets IsError = true, StatusMessage = exception message
```

**Verification during development:** `assembly.GetManifestResourceNames()` lists all embedded resources — useful for debugging key mismatches.

---

## 10. Polly Retry — Detailed Configuration

```
Policy trigger:     HttpPolicyExtensions.HandleTransientHttpError()
                    + OrResult(r => r.StatusCode == RequestTimeout)

Retry count:        3

Delay schedule:     Exponential backoff
                    Attempt 1 →  2 seconds  (2^1)
                    Attempt 2 →  4 seconds  (2^2)
                    Attempt 3 →  8 seconds  (2^3)

Total max wait:     14 seconds before final failure propagates

NOT retried:        401 Unauthorized (credentials error — retrying is pointless)
                    400 Bad Request  (payload error — retrying is pointless)
                    Any 2xx          (success)

Retry scope:        Applied at the IHttpClientFactory named-client pipeline level
                    in MauiProgram.cs — ApiService is unaware of retry logic

onRetry callback:   Debug.WriteLine for diagnostic output during development
```

---

## 11. Unit Test Plan — VendSys.Maui.Tests

### Packages
- `NUnit` 4.x
- `NUnit3TestAdapter`
- `Microsoft.NET.Test.Sdk`
- `NSubstitute`
- `Microsoft.Extensions.Http` (for mocking `HttpMessageHandler`)

### 11.1 MainViewModelTests

| Test | Verifies |
|------|----------|
| `SendDexACommand_WhenExecuted_SetsIsBusyTrue` | IsBusy becomes true at start of execution |
| `SendDexACommand_OnSuccess_SetsIsBusyFalse` | IsBusy resets to false after success |
| `SendDexACommand_OnSuccess_SetsSuccessMessage` | StatusMessage contains machine identifier |
| `SendDexACommand_OnSuccess_IsErrorFalse` | IsError is false on success |
| `SendDexACommand_OnFailure_SetsErrorMessage` | StatusMessage set to ApiResult.ErrorMessage |
| `SendDexACommand_OnFailure_IsErrorTrue` | IsError is true on failure |
| `SendDexACommand_OnFailure_RaisesOnSendFailedEvent` | Event raised with error message |
| `SendDexBCommand_WhileAIsRunning_CannotExecute` | CanExecute false on B while A in progress |
| `SendDexACommand_WhileBIsRunning_CannotExecute` | CanExecute false on A while B in progress |
| `SendDexACommand_AfterCompletion_BothButtonsReEnabled` | CanExecute true on both after finish |

### 11.2 ApiServiceTests

| Test | Verifies |
|------|----------|
| `SendDexFileAsync_OnSuccess_ReturnsApiResultSuccess` | 200 → ApiResult.IsSuccess = true |
| `SendDexFileAsync_On500_ReturnsApiResultFailure` | 500 → IsSuccess = false, error message contains status |
| `SendDexFileAsync_On401_ReturnsApiResultFailure` | 401 → IsSuccess = false (not retried) |
| `SendDexFileAsync_SetsBasicAuthHeader` | Authorization header present and correctly encoded |
| `SendDexFileAsync_PostsToCorrectEndpoint` | Request URI = /vdi-dex?machine=A |
| `SendDexFileAsync_UsesTextPlainContentType` | Content-Type = text/plain |
| `SendDexFileAsync_OnNetworkError_ReturnsApiResultFailure` | HttpRequestException → IsSuccess = false |
| `SendDexFileAsync_OnTransientError_RetriesUpToThreeTimes` | Handler called 4x (1 attempt + 3 retries) |

---

## 12. Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| `IAsyncRelayCommand` from CommunityToolkit.Mvvm | Built-in `IsRunning`/`CanExecuteChanged` eliminates manual busy-flag plumbing on the command itself |
| Both buttons share a single `IsBusy` property | One in-flight request at a time; disabling both prevents race conditions with two simultaneous POSTs |
| `ApiResult` record instead of exceptions for flow control | Exceptions are for exceptional conditions; a 401 or 500 from the API is an expected outcome that the VM handles gracefully |
| Error alert raised via event, not Shell call in VM | Keeps the ViewModel free of MAUI UI references — fully testable with NSubstitute without a running MAUI host |
| Polly attached at `IHttpClientFactory` pipeline, not in `ApiService` | Retry behaviour is infrastructure concern; `ApiService` stays focused on request construction and response mapping |
| DEX text as `EmbeddedResource`, not `MauiAsset` | `MauiAsset` is platform-copied at build; `EmbeddedResource` is compiled into the assembly and always accessible via `GetManifestResourceStream`, regardless of platform file-system restrictions |
| `ApiConstants` static class | Single source of truth for all strings; compiler catches typos; satisfies the no-magic-strings convention |
| No hardcoded colors in XAML | All colors via `StaticResource`; changing the palette requires editing one file |
