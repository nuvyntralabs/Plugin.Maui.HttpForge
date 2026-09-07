# Plugin.Maui.HttpForge — roadmap

HttpForge 1.1.0 is a source-generated REST client: HTTP verbs, path/query/body/headers, JSON, multipart, query objects, collection formats, naming presets, `[Timeout]` / `[Url]` / `[PathPrefix]`, optional segments, `[QueryName]` / `[FormObject]`, streaming, request compression, `IApiResponse<T>`, DI, and analyzers.

Retry, cache, token refresh, and resumable upload are **not** on this list. Those stay in [ApiResilience](https://www.nuget.org/packages/Plugin.Maui.ApiResilience), [ApiCache](https://www.nuget.org/packages/Plugin.Maui.ApiCache), [SecureSession](https://www.nuget.org/packages/Plugin.Maui.SecureSession), and [SmartUpload](https://www.nuget.org/packages/Plugin.Maui.SmartUpload).

---

## Status

| Item | Status | Package / API |
| --- | --- | --- |
| Query objects, collection formats, camel/snake/kebab | In 1.1.0 | `[Query]`, `CollectionFormat`, `UrlParameterKeyFormatter` |
| `[Timeout]`, `[Url]`, `[PathPrefix]`, optional route segments | In 1.1.0 | Core attributes |
| `[QueryName]` valueless flags, `[FormObject]` | In 1.1.0 | Core attributes |
| SSE / `IAsyncEnumerable<T>` / JSON Lines | In 1.1.0 | `IAsyncEnumerable<T>`, `[Body(BodySerializationMethod.JsonLines)]` |
| Request-body compression | In 1.1.0 | `RequestBodyCompression`, `[CompressRequest]` |
| Authorization header value getter | In 1.1.0 | `AuthorizationHeaderValueGetter` (attach only) |
| Newtonsoft.Json / XML packages | In 1.1.0 | `Plugin.Maui.HttpForge.NewtonsoftJson`, `Plugin.Maui.HttpForge.Xml` |
| First-party stub testing package | In 1.1.0 | `Plugin.Maui.HttpForge.Testing` |
| Reflection fallback package | Not planned | Stay generated-only |

`[Url]` accepts a runtime URL and changes the trust boundary. Validate the value before calling (SSRF risk if the host accepts untrusted input).

`AuthorizationHeaderValueGetter` attaches a header. It does not refresh tokens. Compose SecureSession or ApiResilience for 401 retry.

---

## Already out of scope (use siblings)

| Need | Package |
| --- | --- |
| Retry, circuit breaker, offline POST queue, 401 refresh | Plugin.Maui.ApiResilience |
| CacheFirst / NetworkFirst / SWR | Plugin.Maui.ApiCache |
| Auth session / biometric token lock | Plugin.Maui.SecureSession |
| Chunked resume after process death | Plugin.Maui.SmartUpload |
| Connectivity / captive portal | Plugin.Maui.NetworkMonitor |

---

## Sources

- [HttpForge vs Refit](refit-comparison.md)
- [README — HttpForge vs Refit](../README.md#httpforge-vs-refit)
- [Refit detailed analysis](Refit_Detailed_Analysis.md)
- [Refit documentation](https://reactiveui.github.io/refit/)
