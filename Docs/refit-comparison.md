# HttpForge 1.1.0 vs Refit 15

Both libraries declare a REST API as a C# interface and generate the `HttpClient` implementation. [Refit](https://github.com/reactiveui/refit) is the mature, general-purpose client. HttpForge is a MauiEssentials-shaped subset for Android, iOS, Mac Catalyst, and Windows. It is not a drop-in replacement.

Registration is familiar:

| | HttpForge | Refit |
| --- | --- | --- |
| Manual | `RestService.For<IUserApi>(http)` | `RestService.For<IUserApi>(http)` |
| DI | `AddHttpForgeClient<IUserApi>(...)` | `AddRefitClient<IUserApi>()` |
| MAUI host | `UseHttpForge()` | no MAUI-specific host API |

---

## Contract surface

| Capability | HttpForge 1.1.0 | Refit 15 |
| --- | --- | --- |
| Interface + `[Get]` / `[Post]` / `[Put]` / `[Delete]` / `[Patch]` / `[Head]` | Yes | Yes |
| Path parameters, `[AliasAs]`, `[Query]`, `[Body]`, `[Header]` / `[Headers]` | Yes | Yes |
| Query objects, collection formats (`Multi` / `Csv` / `Ssv` / `Tsv` / `Pipes`) | Yes | Yes |
| camel / snake / kebab query keys | `UrlParameterKeyFormatter` | Yes |
| Optional route segments (`{id?}`) | Yes | Yes |
| `[Timeout]`, `[Url]`, `[PathPrefix]` | Yes | Yes |
| `[QueryName]` valueless flags, `[FormObject]` | Yes | Yes |
| Multipart (`StreamPart` / `ByteArrayPart` / `FileInfoPart`) | Yes | Yes |
| `CancellationToken` | Yes | Yes |
| `Task<IApiResponse<T>>` | Yes | Yes (`IApiResponse<T>` / `ApiResponse<T>`) |
| SSE / `IAsyncEnumerable<T>` / JSON Lines | Yes | Yes |
| Request-body compression | gzip / brotli | Yes (15.2+) |
| Authorization header value getter | `AuthorizationHeaderValueGetter` (attach only; absolute URI) | Yes |
| HTTP vs transport exceptions | `ApiException` / `ApiRequestException` | `ApiException` / `ApiRequestException` |
| Source-generated client **and** request construction | Yes | Yes (Refit 14+) |
| Compile-time diagnostics | HFG001–HFG010 | Yes (richer analyzer set) |
| System.Text.Json default | Yes | Yes |
| `JsonSerializerContext` / AOT hook | Yes | Yes |
| `IHttpClientFactory` + `DelegatingHandler` | Yes | Yes (`Refit.HttpClientFactory`) |
| Newtonsoft.Json / XML | `Plugin.Maui.HttpForge.NewtonsoftJson`, `Plugin.Maui.HttpForge.Xml` | `Refit.Newtonsoft.Json`, `Refit.Xml` |
| First-party stub testing | `Plugin.Maui.HttpForge.Testing` | `Refit.Testing` |
| Reflection fallback | No (generated-only) | `Refit.Reflection` |
| Retry, circuit, offline queue | Compose [ApiResilience](https://www.nuget.org/packages/Plugin.Maui.ApiResilience) | Compose Polly / Microsoft resilience |
| GET response cache | Compose [ApiCache](https://www.nuget.org/packages/Plugin.Maui.ApiCache) | Host-owned |
| Tokens / 401 refresh | Compose [SecureSession](https://www.nuget.org/packages/Plugin.Maui.SecureSession) or ApiResilience | Host-owned |
| Resumable upload | Compose [SmartUpload](https://www.nuget.org/packages/Plugin.Maui.SmartUpload) | Host-owned |
| Target matrix | `net10.0` + Android / iOS / Mac Catalyst / Windows | Broader (.NET 8–11, WinUI, Blazor, Uno, .NET Framework) |

Do not treat this table as superiority. Refit is the right default when the team already uses it, needs a reflection fallback, or targets a broader framework matrix. Prefer HttpForge when you want a generated client that matches the MauiEssentials catalog and chains with those plugins on `IHttpClientBuilder`.

---

## Same shape

```csharp
[Get("/users/{id}")]
Task<User> GetUser(int id);

[Post("/users")]
Task<User> Create([Body] CreateUserRequest request);
```

HttpForge 1.1 also matches the Refit-shaped extras that were missing in 1.0:

```csharp
[PathPrefix("/api/v1")]
public interface IUserApi
{
    [Get("/users")]
    Task<List<User>> Search([Query] UserQuery query, [Query(CollectionFormat.Multi)] int[] ages);

    [Get("/users/{id}/orders/{orderId?}")]
    [Timeout(5_000)]
    Task<List<Order>> GetOrders(int id, int? orderId);

    [Get("/items")]
    Task<List<Item>> List([QueryName] string flag);

    [Get("/events")]
    IAsyncEnumerable<Event> StreamEvents(CancellationToken cancellationToken);
}
```

```csharp
settings.UrlParameterKeyFormatter = UrlParameterKeyFormatter.SnakeCase;
settings.AuthorizationHeaderValueGetter = (request, ct) => tokenStore.GetAccessTokenAsync(ct);
settings.RequestBodyCompression = RequestBodyCompression.Gzip;
```

`AuthorizationHeaderValueGetter` sees an absolute URI (`HttpClient.BaseAddress` + relative path). It attaches a header only. 401 refresh stays on SecureSession or ApiResilience.

`[Url]` replaces the method path with a runtime string or `Uri`. Validate that value (SSRF risk).

---

## What HttpForge does not copy

Refit’s analysis ([Refit_Detailed_Analysis.md](Refit_Detailed_Analysis.md)) is the design source. HttpForge keeps the declarative contract and source generation, and leaves mobile networking problems to sibling packages.

There is no `Refit.Reflection` equivalent. Unsupported shapes fail at compile time (HFG001–HFG010). Write that one `HttpClient` method by hand.

---

## Packages

| Need | HttpForge | Refit |
| --- | --- | --- |
| Core generated client | `Plugin.Maui.HttpForge` | `Refit` |
| DI / `IHttpClientFactory` | included | `Refit.HttpClientFactory` |
| Newtonsoft.Json | `Plugin.Maui.HttpForge.NewtonsoftJson` | `Refit.Newtonsoft.Json` |
| XML (XXE-safe) | `Plugin.Maui.HttpForge.Xml` | `Refit.Xml` |
| Test stubs | `Plugin.Maui.HttpForge.Testing` | `Refit.Testing` |
| Reflection fallback | not planned | `Refit.Reflection` |

```bash
dotnet add package Plugin.Maui.HttpForge
dotnet add package Plugin.Maui.HttpForge.Testing
dotnet add package Plugin.Maui.HttpForge.NewtonsoftJson
dotnet add package Plugin.Maui.HttpForge.Xml
```
