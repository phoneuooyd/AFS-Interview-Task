AFS .NET Developer Test
Goal

Build a small ASP.NET Core Web API service that accepts a text string and returns a translation using an external translation provider (e.g., l33tsp34k). The solution must be written as if it will be maintained and extended by other developers.

Functional Requirements
1) Translate endpoint

Create an endpoint:

POST /api/translate
Request body:
text (string, required, 1–500 chars)
translator (string, required) — e.g. "leetspeak"
Response:
translatedText (string)
translator (string)
requestId (GUID)
durationMs (int)
Translator provider
Use FunTranslations API as the external provider.
You may implement a single translator initially (leetspeak), but design for easy addition of new translators.
Reliability requirement (important)

Handle:

non-200 responses
timeouts
rate limiting (429)

You do not need a full resilience library, but you must show sane handling and return meaningful API errors.

2) Audit logging (mandatory)

Record every translation attempt in a database table.

Minimum fields:
Id (GUID)
CreatedAtUtc
Translator
InputText (truncate/store safely)
OutputText (nullable if failed)
ProviderStatusCode
IsSuccess
ErrorMessage (nullable)
DurationMs
CorrelationId (GUID)
3) Query endpoint (mandatory)

Create an endpoint to view logs:

GET /api/translation-logs
Required features:
Pagination:
page
pageSize
Filtering:
translator
isSuccess
fromUtc, toUtc
free-text search on input (basic contains is fine)
Return:
total count
page metadata
items list
Non-Functional Requirements
Architecture
Use clean separation (e.g., Controller → Service → Provider/Repository)
Dependency injection
Meaningful naming and structure
DTOs vs domain models (don’t leak EF entities as API contracts)
Data
Use EF Core with SQLite (preferred) or SQL Server LocalDB.
Provide migration scripts or simple setup instructions.
Testing (mandatory)
At least 2 unit tests:
one for translation service behaviour (success or failure path)
one for logging/query logic (or controller validation)

Mock the external API in tests (don’t call FunTranslations in tests).

Documentation

Provide a short README with:

how to run
how to test
a brief explanation of design decisions
how you would add a new translator provider
Optional Extras (only if time permits)
Authentication (simple JWT or basic auth)
Swagger / OpenAPI
Rate-limit handling with retry/backoff
A simple UI page (not required)

Źródło:

jaki projekt startowy w vs2022 wybrać? 

W tym zadaniu wybór nie jest przypadkowy — wpływa bezpośrednio na to, jak łatwo spełnisz wymagania (architektura, testy, DI, EF Core).

Najlepszy wybór

👉 ASP.NET Core Web API

W Visual Studio 2022:

Create new project
wybierz: ASP.NET Core Web API
Konfiguracja projektu (ważne)

Podczas tworzenia ustaw:

.NET 8 (LTS) lub nowszy (jeśli dostępny)
✅ Use controllers (nie minimal API)
✅ Enable OpenAPI (Swagger) – ułatwi testowanie
❌ Enable HTTPS – opcjonalnie (możesz zostawić)
❌ Use top-level statements – możesz zostawić, ale lepiej wyłączyć dla czytelności
Dlaczego NIE Minimal API?

Można, ale:

gorzej wygląda pod kątem „clean architecture”
trudniej pokazać separację: Controller → Service → Provider
rekruterzy często wolą klasyczne podejście
Zalecana struktura (od razu pod zadanie)

Po utworzeniu projektu rozbij go na warstwy:

/Controllers
    TranslateController.cs
    TranslationLogsController.cs

/Services
    ITranslationService.cs
    TranslationService.cs

/Providers
    ITranslatorProvider.cs
    LeetSpeakProvider.cs

/Repositories
    ITranslationLogRepository.cs
    TranslationLogRepository.cs

/Models
    (Domain)
    TranslationLog.cs

/DTOs
    TranslateRequest.cs
    TranslateResponse.cs
    TranslationLogDto.cs

/Data
    AppDbContext.cs
Dodatkowe projekty (opcjonalnie, ale dobrze wygląda)

Jeśli chcesz zrobić bardziej "seniorowo":

Solution:

- Api (ASP.NET Core Web API)
- Application (Services, DTOs)
- Infrastructure (EF Core, Providers)
- Domain (Modele)
- Tests (xUnit / NUnit)

Ale jeśli masz ograniczony czas:
👉 zrób wszystko w jednym projekcie, ale z dobrą strukturą folderów.

Testy

Dodaj drugi projekt:

👉 xUnit Test Project (.NET)

i:

mockuj provider (np. Moq)
testuj serwis, nie kontroler bezpośrednio
TL;DR

Najprostsza i poprawna opcja:

✔️ ASP.NET Core Web API
✔️ Controllers (nie minimal API)
✔️ .NET 8
✔️ Swagger włączony
✔️ osobny projekt testowy