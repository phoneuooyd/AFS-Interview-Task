# Plan implementacji — AFS .NET Translation API

---

## Etap 1 — Struktura projektu i szkielet

**Cel:** Poprawna architektura od pierwszego commita.

```
TranslationApi/
├── TranslationApi.sln
├── src/
│   └── TranslationApi.Web/              # ASP.NET Core Web API
│       ├── Controllers/
│       │   └── TranslationController.cs
│       ├── DTOs/
│       │   ├── TranslateRequest.cs
│       │   ├── TranslateResponse.cs
│       │   ├── TranslationLogDto.cs
│       │   └── PagedResult.cs
│       ├── Domain/
│       │   └── TranslationLog.cs        # EF entity
│       ├── Services/
│       │   ├── ITranslationService.cs
│       │   └── TranslationService.cs
│       ├── Providers/
│       │   ├── ITranslatorProvider.cs
│       │   ├── TranslatorProviderFactory.cs  # OCP: rejestr providerów
│       │   └── FunTranslations/
│       │       ├── FunTranslationsProvider.cs
│       │       └── FunTranslationsOptions.cs
│       ├── Repositories/
│       │   ├── ITranslationLogRepository.cs
│       │   └── TranslationLogRepository.cs
│       ├── Infrastructure/
│       │   └── AppDbContext.cs
│       ├── Middleware/
│       │   └── CorrelationIdMiddleware.cs
│       └── Program.cs
└── tests/
    └── TranslationApi.Tests/
        ├── Services/
        │   └── TranslationServiceTests.cs
        └── Repositories/
            └── TranslationLogRepositoryTests.cs
```

**Kluczowe decyzje architektoniczne:**

- `ITranslatorProvider` — interfejs dla każdego zewnętrznego providera (ISP + OCP)
- `TranslatorProviderFactory` — słownik `string → ITranslatorProvider`, rejestrowany w DI jako `IEnumerable<ITranslatorProvider>` + factory — dodanie nowego translatora = nowa klasa + rejestracja, zero zmian w logice
- `TranslationService` zależy od abstrakcji, nie od konkretnych providerów (DIP)
- EF entity nigdy nie wychodzi poza warstwę repozytorium — mapowanie do DTO w repozytorium lub serwisie

---

## Etap 2 — Fundament (infrastruktura + DI)

**Kolejność:**

1. **Domain model** — `TranslationLog.cs` (EF entity, tylko pola z wymagań)
2. **AppDbContext** + konfiguracja SQLite, `dotnet ef migrations add Initial`
3. **DTOs** — `TranslateRequest` z atrybutami walidacji (`[Required]`, `[StringLength(500, MinimumLength = 1)]`), reszta jako rekordy
4. **Program.cs** — rejestracja wszystkich zależności, `HttpClient` przez `IHttpClientFactory` (named/typed client dla FunTranslations z `BaseAddress` i `Timeout`)
5. **CorrelationIdMiddleware** — generuje/propaguje `X-Correlation-Id` header, przechowuje w `IHttpContextAccessor`

---

## Etap 3 — Core business logic

### 3a. Provider

```csharp
public interface ITranslatorProvider
{
    string TranslatorName { get; }  // "leetspeak"
    Task<string> TranslateAsync(string text, CancellationToken ct);
}
```

`FunTranslationsProvider`:
- Używa `IHttpClientFactory`
- Obsługa błędów:
  - `429` → rzuca `RateLimitException` (custom exception)
  - timeout → rzuca `TranslationTimeoutException`
  - non-200 → rzuca `TranslationProviderException` z kodem statusu
- Deserializuje odpowiedź do lokalnego rekordu (nie leci surowy JSON dalej)

### 3b. Factory

```csharp
public class TranslatorProviderFactory
{
    private readonly IReadOnlyDictionary<string, ITranslatorProvider> _providers;

    public TranslatorProviderFactory(IEnumerable<ITranslatorProvider> providers)
        => _providers = providers.ToDictionary(p => p.TranslatorName, StringComparer.OrdinalIgnoreCase);

    public ITranslatorProvider GetProvider(string name)
        => _providers.TryGetValue(name, out var p) ? p : throw new UnsupportedTranslatorException(name);
}
```

### 3c. TranslationService

Odpowiada za orkiestrację:

1. Pobierz providera z factory
2. Zmierz czas (`Stopwatch`)
3. Wywołaj `provider.TranslateAsync()`
4. Zapisz `TranslationLog` przez repozytorium (zarówno sukces jak i błąd — w bloku `finally`)
5. Zwróć `TranslateResponse` z `requestId = correlationId`

### 3d. Repository

`TranslationLogRepository`:

- `AddAsync(TranslationLog)` — persist log
- `QueryAsync(TranslationLogQuery query)` — zwraca `PagedResult<TranslationLogDto>`
  - Buduje `IQueryable` z opcjonalnymi filtrami (LINQ, bez surowego SQL)
  - `OrderByDescending(x => x.CreatedAtUtc)` → `.Skip().Take()`

---

## Etap 4 — Endpointy i obsługa błędów

### POST /api/translate

- Model validation przez `[ApiController]` (automatyczny 400)
- Mapowanie custom exceptions na kody HTTP w `GlobalExceptionHandler` (`.NET 8 IExceptionHandler` lub middleware):

| Wyjątek | HTTP |
|---|---|
| `UnsupportedTranslatorException` | 400 |
| `RateLimitException` | 429 |
| `TranslationTimeoutException` | 504 |
| `TranslationProviderException` | 502 |

- Response body zawsze w formacie `ProblemDetails`

### GET /api/translation-logs

- Query params bindowane do `TranslationLogQuery` (record z nullable polami)
- Walidacja: `pageSize` max 100, `fromUtc < toUtc`

---

## Etap 5 — Testy jednostkowe

### Test 1 — `TranslationServiceTests`

```
[Fact] GivenValidRequest_WhenProviderSucceeds_ReturnsTranslatedText_AndLogsSuccess()
[Fact] GivenValidRequest_WhenProviderThrows429_LogsFailure_AndRethrows()
[Fact] GivenUnknownTranslator_ThrowsUnsupportedTranslatorException_WithoutCallingProvider()
```

Mocki: `ITranslatorProvider` (Mock), `ITranslationLogRepository` (Mock), `TranslatorProviderFactory` (realna instancja z zamockowanym providerem)

### Test 2 — `TranslationLogRepositoryTests`

```
[Fact] QueryAsync_FilterByIsSuccess_ReturnsOnlyMatchingRecords()
[Fact] QueryAsync_Pagination_ReturnsCorrectPage_AndTotalCount()
[Fact] QueryAsync_FreeTextSearch_FiltersByInputTextContains()
```

Setup: `AppDbContext` z `UseInMemoryDatabase` (lub SQLite in-memory), seed danych przed każdym testem.

**Framework:** xUnit + Moq + FluentAssertions

---

## Etap 6 — Opcjonalne extras (jeśli czas pozwoli)

**Priorytet 1 — Swagger** (szybkie, daje +++ wrażenie):
- `Swashbuckle.AspNetCore` + XML comments na kontrolerze + przykłady w `TranslateRequest`

**Priorytet 2 — Retry/backoff dla 429:**

```csharp
// W FunTranslationsProvider, bez Polly:
if (response.StatusCode == 429)
{
    var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(10);
    throw new RateLimitException(retryAfter);
}
// W serwisie: złap RateLimitException, zwróć 429 z Retry-After headerem
```

**Priorytet 3 — Basic JWT auth** (jeśli naprawdę zostanie czas)

---

## Kolejność wdrożenia (szacowany czas)

| # | Etap | ~czas |
|---|---|---|
| 1 | Struktura + Program.cs + DI szkielet | 30 min |
| 2 | Domain, DbContext, migracja | 20 min |
| 3a–c | Provider + Factory + Service | 45 min |
| 3d | Repository + Query | 30 min |
| 4 | Kontrolery + GlobalExceptionHandler | 30 min |
| 5 | Testy jednostkowe | 40 min |
| 6 | README + Swagger | 20 min |
| **Σ** | | **~3.5h** |

---

## README — kluczowe punkty do opisania

- `dotnet ef database update` przed startem
- `dotnet test` uruchamia testy bez zewnętrznych zależności
- **Jak dodać nowy translator:** utwórz klasę implementującą `ITranslatorProvider`, zarejestruj w DI — factory wykryje automatycznie
- Decyzja: brak Polly świadoma — wymaganie mówi "sane handling", nie pełna odporność
