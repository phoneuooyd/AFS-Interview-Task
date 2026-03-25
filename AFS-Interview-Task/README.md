# AFS .NET Translation API

Web API in .NET 8 for text translation with external providers, structured around clean separation of concerns and auditable request flow.

## Endpoint contract

### `POST /api/translate` or `POST /api/translate/funtranslations`
Request body (FunTranslations flow):
- `text` (string, required, 1-500 chars)
- `translator` (string, required), e.g. `"yoda"` / `"pirate"` / `"leetspeak"` depending on FunTranslations translator.

### `POST /api/translate/rapidapi`
Request body (RapidAPI flow):
- `text` (string, required, 1-500 chars)
- `translator` is intentionally not present in the body. It is fixed to `leetspeak` in code for this provider endpoint.

Response:
- `translatedText` (string)
- `translator` (string)
- `requestId` (GUID)
- `durationMs` (int)

## Provider architecture (SOLID-friendly)

The provider layer is split into two responsibilities:

1. **Translator routing (`TranslatorProviderFactory`)**
   - Chooses provider implementation by `translator` name based on config mapping.
2. **Provider adapters (`ITranslatorProvider`)**
   - Unified contract for every external provider:
   - `Task<string> TranslateAsync(string translator, string text, CancellationToken ct)`

This makes provider swapping/configuration explicit and keeps the API/service layer closed for modification but open for extension.

Additionally, `TranslationService` supports overloaded translation execution:
- by translator mapping (`TranslateAsync(TranslateRequest, ...)`)
- by explicit provider key override (`TranslateAsync(providerKey, translator, text, ...)`)

## Configuration-based routing

`appsettings.json`:

```json
"TranslatorRouting": {
  "Translators": {
    "leetspeak": "rapidapi"
  }
}
```

Meaning:
- incoming `translator=leetspeak` is routed to provider with key `rapidapi`.
- change to `funtranslations` to swap backend without changing endpoint/service logic.

## Currently implemented providers

- `funtranslations` (`FunTranslationsProvider`) - generic FunTranslations endpoint (`/{translator}.json`)
- `rapidapi` (`RapidApiLeetSpeakDecoderProvider`) - leetspeak decode implementation for fallback scenario

> Requirement note: FunTranslations is implemented as the primary external provider and architecture supports adding more translators/providers with minimal code changes.

## How to add a new translator/provider

1. Implement `ITranslatorProvider` with a unique `ProviderKey`.
2. Register it in DI (`Program.cs`) as `ITranslatorProvider`.
3. Add/adjust `TranslatorRouting:Translators` mapping in config.
4. (Optional) if provider supports only selected translators, validate input in provider.

No controller/service changes are required.

## Run locally

```bash
dotnet restore
dotnet run --project AFS-Interview-Task/AFS-Interview-Task.csproj
```

## Tests

```bash
dotnet test
```
