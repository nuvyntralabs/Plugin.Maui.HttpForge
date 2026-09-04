# Plugin.Maui.HttpForge — roadmap

HttpForge 1.0.0 is a source-generated REST client: HTTP verbs, path/query/body/headers, JSON, multipart, `IApiResponse<T>`, DI, and analyzers.

This document lists **features that are not in v1**. They come from the Refit comparison in the [README](../README.md#httpforge-vs-refit) and the design notes in [Refit_Detailed_Analysis.md](Refit_Detailed_Analysis.md).

Retry, cache, token refresh, and resumable upload are **not** on this list. Those stay in [ApiResilience](https://www.nuget.org/packages/Plugin.Maui.ApiResilience), [ApiCache](https://www.nuget.org/packages/Plugin.Maui.ApiCache), [SecureSession](https://www.nuget.org/packages/Plugin.Maui.SecureSession), and [SmartUpload](https://www.nuget.org/packages/Plugin.Maui.SmartUpload).

Dates are not commitments. Order can change as hosts ask for a surface.

---

## Not in v1

| Item | Status | Refit today | Planned HttpForge direction |
| --- | --- | --- | --- |
| Query objects, collection formats, camel/snake/kebab | Not in v1 | Yes | Next contract increment |
| `[Timeout]`, `[Url]`, `[PathPrefix]`, optional route segments | Not in v1 | Yes | Next contract increment |
| `[QueryName]` valueless flags, `[FormObject]` | Not in v1 | Yes | Next contract increment |
| SSE / `IAsyncEnumerable<T>` / JSON Lines | Not in v1 | Yes | Later — streaming |
| Request-body compression | Not in v1 | Yes (15.2+) | Later — transport convenience |
| Authorization header value getter | Not in v1 | Yes | Later — or keep composing ApiResilience / SecureSession |
| Newtonsoft.Json / XML packages | Not in v1 | `Refit.Newtonsoft.Json`, `Refit.Xml` | Optional packages only if hosts need them |
| Reflection fallback package | Not planned | `Refit.Reflection` | Stay generated-only |
| First-party stub testing package | Not in v1 | `Refit.Testing` | Later — `HttpForge.Testing` |

---

## 1. Query objects, collection formats, naming

**Missing in v1:** a parameter that is not a path token becomes a single query key. There is no object flatten, no `ages=1&ages=2` vs `ages=1,2`, and no library-wide camel/snake/kebab policy.

Intended shape:

```csharp
public class UserQuery
{
    public string? Name { get; set; }
    public int Page { get; set; }
}

[Get("/users")]
Task<List<User>> Search([Query] UserQuery query);

[Get("/users")]
Task<List<User>> SearchAges([Query(CollectionFormat.Multi)] int[] ages);
```

Also:

```csharp
settings.UrlParameterKeyFormatter = UrlParameterKeyFormatter.SnakeCase;
```

or a `HttpForgeSettings` naming preset (`CamelCase`, `SnakeCase`, `KebabCase`) applied to query keys (and optionally JSON, if the host has not set `JsonSerializerOptions`).

---

## 2. `[Timeout]`, `[Url]`, `[PathPrefix]`, optional route segments

**Missing in v1:** every method uses the attribute path as written. There is no per-call absolute URL, no shared prefix, no optional `{id?}` segment, and no per-method timeout.

Intended shape:

```csharp
[PathPrefix("/api/v1")]
public interface IUserApi
{
    [Get("/users/{id}")]
    [Timeout(5_000)]
    Task<User> GetUser(int id);

    [Get("/users/{id}/orders/{orderId?}")]
    Task<List<Order>> GetOrders(int id, int? orderId);

    [Get("/")]
    Task<User> GetFromAbsolute([Url] string url);
}
```

`[Timeout]` should cancel that call only. App-wide timeouts stay on `HttpClient` or ApiResilience.

`[Url]` must stay explicit: a runtime URL changes the trust boundary. Document SSRF risk next to the attribute.

---

## 3. `[QueryName]` valueless flags and `[FormObject]`

**Missing in v1:** every query value is `name=value`. Multipart/form fields are one parameter each.

Intended shape:

```csharp
[Get("/items")]
Task<List<Item>> List([QueryName] string flag);
// GET /items?archived

[Multipart]
[Post("/profile")]
Task Save([FormObject] ProfileForm form);
// name=...&address.city=...
```

---

## 4. SSE / `IAsyncEnumerable<T>` / JSON Lines

**Missing in v1:** return types are `Task`, `Task<T>`, `Task<IApiResponse<T>>`, and `Task<HttpResponseMessage>`.

Intended shape:

```csharp
[Get("/events")]
IAsyncEnumerable<Event> StreamEvents(CancellationToken cancellationToken);

[Post("/batch")]
Task Send([Body(BodySerializationMethod.JsonLines)] IEnumerable<Item> items);
```

This needs a streaming serializer path, not only `ReadAsStringAsync`. Keep it off the default JSON code path until the generator can emit it without reflection.

---

## 5. Request-body compression

**Missing in v1:** request bodies are sent as the serializer produced them.

Intended shape: a setting or attribute that gzip/brotli-encodes `[Body]` content and sets `Content-Encoding`. Skip multipart and already-compressed streams. Measure allocations on mobile before making it the default.

---

## 6. Authorization header value getter

**Missing in v1:** `[Headers("Authorization: Bearer")]` is a static string. There is no `AuthorizationHeaderValueGetter`.

Two acceptable outcomes:

1. **Library hook** (Refit-like):

```csharp
settings.AuthorizationHeaderValueGetter = (request, ct) => tokenStore.GetAccessTokenAsync(ct);
```

2. **Keep composing** ApiResilience / SecureSession `DelegatingHandler`s, and document that as the MAUI path.

Do not add a second token-refresh implementation inside HttpForge.

---

## 7. Newtonsoft.Json / XML packages

**Missing in v1:** only `SystemTextJsonContentSerializer`. Hosts can already swap `IHttpContentSerializer`.

Optional later packages:

```text
Plugin.Maui.HttpForge.NewtonsoftJson
Plugin.Maui.HttpForge.Xml
```

Do not take a Json.NET or XML dependency in the core package. XML defaults must stay XXE-safe.

---

## 8. Reflection fallback — not planned

Refit ships `Refit.Reflection` for shapes the generator cannot emit.

HttpForge stays **generated-only**. Unsupported shapes fail at compile time (HFG00x). A reflection package would fight trimming/AOT and the v1 design. If a host needs an irregular API, they write that one `HttpClient` method by hand.

---

## 9. First-party stub testing package

**Missing in v1:** tests use a raw `HttpMessageHandler` stub.

Intended later package: `Plugin.Maui.HttpForge.Testing`.

```csharp
var http = new StubHttp
{
    { Route.Get("/users/{id}"), Reply.With(new User(7, "octocat")) }
};

var api = http.CreateClient<IUserApi>("https://api.example.com");
var user = await api.GetUser(7);
await http.VerifyAllCalledAsync();
```

Useful extras: typed body inspection, latency, transport faults. This is more valuable than asking every host to mock `HttpMessageHandler`.

---

## Suggested delivery order

1. Query objects, collection formats, naming presets
2. `[Timeout]`, `[Url]`, `[PathPrefix]`, optional segments
3. `[QueryName]`, `[FormObject]`
4. `HttpForge.Testing`
5. Authorization getter **or** documented SecureSession/ApiResilience recipe (choose one)
6. Streaming (`IAsyncEnumerable`, JSON Lines, SSE)
7. Request-body compression
8. Newtonsoft / XML packages only if hosts ask

Each increment should add generator tests and README examples. Do not widen the attribute surface without an analyzer for invalid combinations.

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

- [README — HttpForge vs Refit](../README.md#httpforge-vs-refit)
- [Refit detailed analysis](Refit_Detailed_Analysis.md)
- [Refit documentation](https://reactiveui.github.io/refit/)
