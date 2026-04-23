# AI-Assisted Development Analysis

Before interacting with the AI, I documented in a notepad all the components that a .NET API project with a MAUI client would require. I then read through the challenge requirements and selected the architecture best suited for this type of solution, prioritising future scalability.

---

## Technologies Used

**Backend:**
- ASP.NET Core 9 Minimal API
- Entity Framework Core 9
- SQL Server LocalDB
- Serilog (console + rolling file sink)
- NUnit, Moq
- Docker / docker-compose

**Client:**
- .NET 9 MAUI (Windows WinUI 3, Android, iOS)
- MAUI Community Toolkit
- CommunityToolkit.Mvvm
- Polly (retry policy), IHttpClientFactory
- NUnit, Moq

**Architecture and Patterns:**
- Clean Architecture (Domain, Application, Infrastructure, API)
- Domain-Driven Design (DDD)
- SOLID principles, native .NET dependency injection
- MVVM pattern with XAML
- Stored Procedures (`SaveDEXMeter` and `SaveDEXLaneMeter`) for all database write operations

---

## AI-Assisted Development Strategy

With that context defined, I applied my experience as a Business Analyst to break down the requirements one by one. I worked alongside the AI following a deliberate strategy: I first had it read the specification document and generate a `context.md` file, which acts as persistent memory throughout the project, preventing the model from losing track of key details between sessions.

Before generating any technical document, I explicitly defined the coding conventions the AI was required to follow across all generated files: PascalCase for classes and methods, the `_underscore` prefix for private fields, the `Async` suffix on all asynchronous methods, XML doc comments (`///`) on all public members, file-scoped namespaces, and a prohibition on magic strings. Establishing these rules upfront prevented style inconsistencies throughout the codebase.

I then asked the AI to produce an `architecture.md` file reflecting the technical design I had already defined: Clean Architecture with four layers (Domain, Application, Infrastructure, API), Domain-Driven Design, SOLID principles, native dependency injection, the MVVM pattern for the MAUI client, and NUnit for testing. The AI did not make architectural decisions autonomously — it translated my specifications into a structured document, including a Mermaid dependency diagram.

For the database layer, the strategy was to use Entity Framework Core not as a direct-write ORM, but as a schema manager: EF Core is responsible for defining and creating tables via migrations and for executing stored procedures through `ExecuteSqlRawAsync`. All write operations are routed exclusively through the `SaveDEXMeter` and `SaveDEXLaneMeter` stored procedures, in full compliance with the challenge requirements. `DbSet` instances in the EF context are reserved exclusively for read operations. This design was documented in `database-design.md` with the full normalised schema, data types, table relationships, and migration strategy.

For logging, Serilog was configured with a dual-sink setup: a console sink for local development and a rolling file sink writing to `logs/api-.log` for production environments, capturing the timestamp, machine parameter, HTTP status code, and request duration for every incoming call. A global error handling middleware was also defined to return consistent JSON error responses containing `error` and `traceId` fields, ensuring that no unhandled exception reaches the client in an unexpected format.

Regarding API documentation, I originally considered adding Swagger support via Swashbuckle to facilitate manual endpoint testing. However, the challenge document explicitly stated that the API should remain as minimal as possible, so I decided not to include it in the final submission. In a real production context, this would be the first extension to add.

Once the architectural structure was in place, I asked the AI to generate a `backlog.md` containing 19 features ordered sequentially, following a Scrum-like methodology. Each feature includes a title, description, acceptance criteria, and the affected layer. This allows any AI agent or human developer to pick up the backlog and execute tasks incrementally and predictably until the application is fully functional, without relying on prior context.

Before writing a single line of business logic, I instructed the AI to run the native .NET SDK scaffolding to create the solution and all projects. This decision is grounded in years of development experience: when an AI generates large volumes of code from scratch, it tends to introduce errors in configuration files such as `.csproj` files, project references, and namespace structures. By using the official Microsoft scaffolding via `dotnet new`, the SDK automatically generates a fully functional blank template that compiles cleanly from the very first build. Development then proceeds on top of that foundation, exactly as a human developer would work. This approach not only eliminates early compilation errors but also reduces token consumption by avoiding unnecessary boilerplate regeneration.

The solution was organised into separate physical folders from the outset: `src/Backend/` for the four API layers, `src/Client/` for the MAUI project, and `tests/Backend/` and `tests/Client/` for their respective test projects. This separation is reflected both in the file system and in Visual Studio solution folders, preventing confusion when navigating between projects of entirely different nature.

To run both projects simultaneously during development, Visual Studio was configured with multiple startup projects, with `VendSys.Api` listed first and `VendSys.Maui` second, ensuring the API is available before the client attempts to connect. The Polly retry policy configured on the client side (3 retries with exponential backoff) acts as a safety net for cases where the API has not yet finished starting up.

With the scaffolding in place and NuGet packages installed, development proceeded feature by feature in accordance with the backlog. By keeping each feature tightly scoped, I was able to perform a code review limited to only the classes affected by each change, avoiding the need to read through hundreds of accumulated lines of code. This workflow allowed me to read the generated code, understand the AI's explanation, apply corrections where necessary, and reformulate instructions when required — for instance, in the unit tests where the AI was referencing files outside the solution scope, which was identified and corrected during the review cycle.

---

## Final Code Review and Optimisations

Upon completing the development, I conducted a general code review of the entire solution, identifying improvement opportunities beyond functional correctness. The most significant change was replacing Entity Framework Core for stored procedure execution with a dedicated `ISqlExecutor` abstraction that executes raw SQL directly over an ADO.NET connection. While EF Core fulfils the role adequately, it introduces overhead through object materialisation and entity change tracking, neither of which adds value in a pure write flow like this one. By working directly with `SqlCommand` and `SqlConnection`, those intermediate layers are removed entirely. This translates to a measurable reduction in milliseconds per operation which, in scenarios involving large-batch data processing — such as the concurrent ingestion of DEX reports from multiple vending machines — can represent a significant improvement in overall system throughput. This abstraction also preserves the integrity of the infrastructure layer contract, as `ISqlExecutor` remains an interface defined in the Application layer and implemented in Infrastructure, fully respecting the Clean Architecture dependency rules.

As time permitted, I also implemented the optional table-clearing feature described in the challenge spec. A dedicated button on the MAUI interface triggers a call to the `ClearAllData` stored procedure, which truncates both tables in the correct foreign key order. To prevent accidental data loss, the action is guarded by a confirmation dialog that requires explicit user acknowledgement before the operation proceeds. This is a standard UX safeguard for any destructive operation and ensures the feature remains safe to include in the UI without the risk of unintended deletions.

I also reviewed the database schema to ensure the tables were correctly indexed for maximum insert performance at scale. At this stage, the existing primary key and foreign key constraints provide sufficient indexing for the current write patterns, and no additional indexes are required. This assessment will need to be revisited once query methods beyond bulk insertion are introduced — for example, filtering DEX records by machine, date range, or product identifier would likely benefit from dedicated non-clustered indexes on those columns.

---

## Source Code and Additional Projects

The complete solution is publicly available on my personal GitHub repository:
[github.com/diegorenatob/VenDexChallenge](https://github.com/diegorenatob/VenDexChallenge)

The repository includes the full commit history, which reflects how the solution was built and refined incrementally — from initial scaffolding through architecture documents, feature implementation, and final optimisations. Reviewers are welcome to browse the commit log as a transparent record of the AI-assisted development process described in this document.

I also invite you to explore my repository, in particular:
[github.com/diegorenatob/AlohaPDF](https://github.com/diegorenatob/AlohaPDF)

AlohaPDF is an open source library I authored for generating PDF documents in .NET MAUI using the SkiaSharp graphics engine. It is currently the only fully free and open source PDF generation library available for .NET MAUI that can be used without a paid licence — for both open source and commercial projects alike.

Finally, the full conversation log with the Claude AI tool (Anthropic) is attached as a separate Markdown file. It provides a complete and unedited record of every prompt used throughout this project, along with the AI's responses, as requested in the challenge submission guidelines.
