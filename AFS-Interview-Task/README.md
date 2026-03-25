# AFS .NET Translation API

Web API in .NET 8 for text translation with external providers.

## Endpoint contract

### `POST /api/translate`
Request body:
- `text` (string, required, 1-500 chars)
- `translator` (string, conditionally required)

Response:
- `translatedText` (string)
- `translator` (string)
- `requestId` (GUID)
- `durationMs` (int)

## Conditional request rules (API + Swagger)

Body requirements depend on configured default provider (`TranslationExecution:DefaultProvider`):

- `funtranslations` -> `translator` is required (e.g. `leetspeak`, `pirate`, etc.)
- `rapidapi` -> `translator` is optional; API can execute translation with provider-specific overload (`TranslateAsync(text, ct)`).

If `translator` is provided, routing still uses `TranslatorRouting:Translators`.

## Provider architecture

`ITranslatorProvider` supports overloads:

- `TranslateAsync(string text, CancellationToken ct)`
- `TranslateAsync(string translator, string text, CancellationToken ct)`

This enables provider-specific communication contracts while keeping a unified abstraction.

### Implemented providers

- `funtranslations` (`FunTranslationsProvider`) - requires translator-aware overload.
- `rapidapi` (`RapidApiLeetSpeakDecoderProvider`) - supports both overloads.

## Configuration

```json
"TranslationExecution": {
  "DefaultProvider": "rapidapi"
},
"TranslatorRouting": {
  "Translators": {
    "leetspeak": "rapidapi"
  }
}
```

## Extensibility

1. Implement new `ITranslatorProvider`.
2. Decide whether it supports translator-less overload, translator-aware overload, or both.
3. Register in DI and map translators in `TranslatorRouting`.

## Tests

```bash
dotnet test
```
