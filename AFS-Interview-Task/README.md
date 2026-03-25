# AFS .NET Translation API

Web API in .NET 8 for text translation with external providers, structured around clean separation of concerns and auditable request flow.

## Endpoint contract

### `POST /api/translate` (default provider from config)
Request body:
- `text` (string, required, 1-500 chars)
- `translator` (string, optional)

Behavior depends on active provider profile from configuration:
- if provider requires translator, missing value is rejected
- if provider has `DefaultTranslator`, missing value is auto-filled

### `POST /api/translate/funtranslations`
Alias endpoint for the same request contract (can still use global default provider).

### `POST /api/translate/rapidapi`
Provider-specific endpoint:
- `text` (string, required, 1-500 chars)
- provider key and translator are fixed in code (`rapidapi` + `leetspeak`)

Response:
- `translatedText` (string)
- `translator` (string)
- `requestId` (GUID)
- `durationMs` (int)

## Provider architecture (low-boilerplate extension)

1. **Provider selection (`TranslatorProviderFactory`)**
   - resolves provider by name
   - falls back to `LeetSpeakTranslation:Provider`
2. **Shared provider base (`TranslatorProviderBase`)**
   - common translator resolving
   - common validation flow
   - configurable rules (`RequiresTranslator`, `DefaultTranslator`) from appsettings
3. **Provider implementations (`ITranslatorProvider`)**
   - only implement provider-specific translation details in `ExecuteCoreAsync`

## Configuration

```json
"LeetSpeakTranslation": {
  "Provider": "rapidapi",
  "Providers": {
    "rapidapi": {
      "RequiresTranslator": false,
      "DefaultTranslator": "leetspeak"
    },
    "funtranslations": {
      "RequiresTranslator": true
    }
  }
}
```

Swagger request body example for `POST /api/translate` is generated from the active provider profile.

## How to add a new provider

1. Add provider profile in `LeetSpeakTranslation:Providers` in appsettings.
2. Create class inheriting `TranslatorProviderBase` and implement provider-specific translation logic.
3. Override base methods only if behavior differs (`ValidateTranslator`, `NormalizeText`, etc.).
4. Register provider in DI as `ITranslatorProvider` (+ `HttpClient` if needed).

No changes are required in `TranslationService`.

## Run locally

```bash
dotnet restore
dotnet run --project AFS-Interview-Task/AFS-Interview-Task.csproj
```

## Tests

```bash
dotnet test AFS-Interview-Task.sln
```
