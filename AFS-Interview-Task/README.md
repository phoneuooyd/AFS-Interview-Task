# AFS .NET Translation API

A modern, Clean Architecture-inspired Web API built with .NET 8 for translating text via external providers with robust auditing.

## Architecture & Design Decisions

This solution follows a simplified **Clean Architecture** style organized in logical folders within a single project:
- **Domain**: Pure models mapping to the database (e.g., `TranslationLog`).
- **DTOs**: Data Transfer Objects specific for input validations and responses, avoiding database leakage.
- **Repositories**: Encapsulates EF Core operations (filtering, saving).
- **Providers**: Interactions with external services, cleanly separated through the `ITranslatorProvider` interface.
- **Services**: Enforces business rules, timer logging, and data persistence before replying.
- **Controllers & Middleware**: Web layer responsibilities safely isolated.

### Key features

*   **SQLite database**: chosen to enable instant local execution without prerequisites.
*   **Factory Pattern (`TranslatorProviderFactory`)**: used to retrieve the correct API provider without requiring changes to fundamental application code. Embraces OCP (Open-Closed Principle).
*   **Global Logging Middleware**: An approach mapping specific native and custom exceptions (`RateLimitException`) into `ProblemDetails` format (RFC 7807) to standardise error responses.
*   **Decoupled Auditing**: Service catches exceptions and logs both `Success` & `Failed` states including translation duration and `CorrelationId` without relying heavily on 3rd party resilience packages like Polly. 

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Getting Started

1. **Verify EF Core Migrations**: The database file `TranslationApi.db` will be instantiated automatically, but verify setup with:
   ```bash
   dotnet ef database update
   ```
2. **Start the API**:
   ```bash
   dotnet run
   ```
3. Look at the local URL returned by the console. Access `https://localhost:<port>/swagger` to experiment with endpoints using **Swagger UI**.

## How to add a new translator provider

To extend the system and add a newly supported translation mode:
1. Create a class implementing `ITranslatorProvider` interface. Set its `TranslatorName` property (e.g. `minion`).
2. Add your logic to `TranslateAsync(string text, CancellationToken ct)`. You can define new response models in your new class namespace if talking to a new endpoint.
3. Open `Program.cs` and register your provider in Dependency Injection using `.AddHttpClient<ITranslatorProvider, MyNewProvider>()`.
   *The `TranslatorProviderFactory` will automatically extract it and begin supporting requests using your `TranslatorName`*. 

## Testing

We provide xUnit test suites mocking the external logic without touching external quotas.

Run all tests via:
```bash
dotnet test
```
*(Tests use an InMemory SQLite database and mocked services Moq for testing complex HTTP behaviours directly without external calls)*.
