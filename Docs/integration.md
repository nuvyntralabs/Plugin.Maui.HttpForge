# Integrate HttpForge with MauiEssentials HTTP plugins

`Plugin.Maui.HttpForge` is the typed REST contract only. It does not reference ApiResilience, ApiCache, SecureSession, or SmartUpload. Chain those yourself when the host already needs them.

`AddHttpForgeClient<T>()` returns `IHttpClientBuilder`. That is the composition point for `DelegatingHandler`s and Microsoft HTTP pipelines.

Do not add a sibling plugin only because HttpForge is installed. Prefer Polly, MSAL, or a host-owned cache when that is already the org standard.

| Need | Compose | Alternative |
| --- | --- | --- |
| Retry, circuit breaker, offline POST queue | [ApiResilience](https://www.nuget.org/packages/Plugin.Maui.ApiResilience) | Polly / Microsoft.Extensions.Http.Resilience |
| GET response cache (CacheFirst / SWR) | [ApiCache](https://www.nuget.org/packages/Plugin.Maui.ApiCache) | Host-owned cache |
| Tokens / 401 refresh | [SecureSession](https://www.nuget.org/packages/Plugin.Maui.SecureSession) or ApiResilience | MSAL / Auth0 / host-owned handler |
| Chunked resume after process death | [SmartUpload](https://www.nuget.org/packages/Plugin.Maui.SmartUpload) | tus / host-owned chunks |

---

## Pipeline

```text
ViewModel
    → IUserApi (HttpForge-generated)
        → IHttpClientFactory
            → GET cache               (ApiCache handler, optional)
            → Auth / 401 refresh      (SecureSession or ApiResilience)
            → Retry / circuit / queue (ApiResilience or Polly)
            → HttpClient
                → HTTPS API
```

`IHttpClientFactory` invokes handlers in reverse add order. Add resilience first, then cache, so a CacheFirst hit never enters retry.

Register host options first (`UseHttpForge`, `UseApiResilience`, `UseSecureSession`, `UseApiCache`, `UseSmartUpload`), then attach each typed client.

Install only the packages the host needs. HttpForge alone is enough for a typed client.

---

## ApiResilience — retry, circuit, offline queue, 401

[Plugin.Maui.ApiResilience](https://github.com/nuvyntralabs/Plugin.Maui.ApiResilience) wraps the same `HttpClient` the generated client uses.

```csharp
using Plugin.Maui.ApiResilience;
using Plugin.Maui.HttpForge;

builder
    .UseMauiApp<App>()
    .UseHttpForge()
    .UseApiResilience(options =>
    {
        options.Retry.MaxRetryAttempts = 3;
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
        options.OfflineQueue.Enabled = true;
        options.TokenRefresh.Enabled = true;
    });

builder.Services.AddSingleton<IAccessTokenProvider, AuthTokenProvider>();

builder.Services
    .AddHttpForgeClient<IUserApi>(client =>
    {
        client.BaseAddress = new Uri("https://api.example.com");
    })
    .AddApiResilience();
```

Every `IUserApi` call then retries transient failures, trips the circuit per host, queues mutating calls when offline, and refreshes a bearer token once on 401.

Do **not** implement retry inside the HttpForge interface.

If the org already standardized on Polly:

```csharp
builder.Services
    .AddHttpForgeClient<IUserApi>(client =>
    {
        client.BaseAddress = new Uri("https://api.example.com");
    })
    .AddStandardResilienceHandler(); // Microsoft.Extensions.Http.Resilience
```

---

## ApiCache — GET CacheFirst / SWR

[Plugin.Maui.ApiCache](https://github.com/nuvyntralabs/Plugin.Maui.ApiCache) remembers GET responses. For a typed HttpForge client, attach the handler. Do **not** also call `IApiCache.GetAsync` for those same URLs or you will cache twice.

```csharp
using Plugin.Maui.ApiCache;
using Plugin.Maui.ApiResilience;
using Plugin.Maui.HttpForge;

builder
    .UseMauiApp<App>()
    .UseHttpForge()
    .UseApiResilience()
    .UseApiCache(options =>
    {
        options.DefaultExpiration = TimeSpan.FromMinutes(30);
        options.DefaultPolicy = CachePolicy.CacheFirst;
    });

builder.Services
    .AddHttpForgeClient<IUserApi>(client =>
    {
        client.BaseAddress = new Uri("https://api.example.com");
    })
    .AddApiResilience()
    .AddApiCache();
```

`IUserApi.GetUser(id)` then goes through CacheFirst (or the configured policy). Cached responses include `X-ApiCache-Hit`, `X-ApiCache-Stale`, and `X-ApiCache-Policy`.

`IApiCache.GetAsync<T>("/users/1")` is the other entry point — a path-based cache that uses its own named `HttpClient`. Use that when there is no typed interface. Do not wrap an HttpForge call with `IApiCache.GetAsync`.

Invalidate after a local write so the next HttpForge GET is not stale:

```csharp
await cache.InvalidateByPrefixAsync("/users");
```

Offline-first **writes** stay on OfflineSync, not ApiCache.

---

## SecureSession — tokens and session lock

[Plugin.Maui.SecureSession](https://github.com/nuvyntralabs/Plugin.Maui.SecureSession) stores access/refresh tokens (via SecureStoragePlus), attaches `Bearer`, and retries once on 401. HttpForge can attach a static token via `AuthorizationHeaderValueGetter`; it does not refresh on 401.

SecureSession targets **Android and iOS**. On Mac Catalyst or Windows, use ApiResilience `IAccessTokenProvider` instead.

Register a **login client without** the session handler (avoids a chicken-and-egg), and the business client **with** it.

```csharp
using Plugin.Maui.ApiResilience;
using Plugin.Maui.HttpForge;
using Plugin.Maui.SecureSession;

builder.Services.AddSingleton<IAuthGateway, ShopAuthGateway>();

builder
    .UseMauiApp<App>()
    .UseHttpForge()
    .UseSecureSession(options =>
    {
        options.AccessTokenRefreshSkew = TimeSpan.FromSeconds(60);
        options.AcceptUnvalidatedTokens = false;
    })
    .UseApiResilience(options =>
    {
        options.TokenRefresh.Enabled = false; // SecureSession already retries 401
        options.OfflineQueue.Enabled = true;
    });

builder.Services
    .AddHttpForgeClient<IAuthApi>(client =>
    {
        client.BaseAddress = new Uri("https://api.example.com");
    });

builder.Services
    .AddHttpForgeClient<IUserApi>(client =>
    {
        client.BaseAddress = new Uri("https://api.example.com");
    })
    .AddSecureSession()
    .AddApiResilience();
```

```csharp
public interface IAuthApi
{
    [Post("/auth/login")]
    Task<TokenBundle> Login([Body] LoginDto request, CancellationToken cancellationToken = default);

    [Post("/auth/refresh")]
    Task<TokenBundle> Refresh([Body] RefreshDto request, CancellationToken cancellationToken = default);
}

public sealed class ShopAuthGateway(IAuthApi api) : IAuthGateway
{
    public bool CanRefresh => true;

    public async Task<AuthResponse> LoginAsync(LoginRequest request, DeviceContext device, CancellationToken ct)
        => new() { Tokens = await api.Login(new LoginDto(request.Username, request.Password), ct) };

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, DeviceContext device, CancellationToken ct)
        => new() { Tokens = await api.Refresh(new RefreshDto(request.RefreshToken), ct), RefreshTokenRotated = true };

    public Task LogoutAsync(LogoutRequest request, CancellationToken ct) => Task.CompletedTask;

    public Task<IReadOnlyList<RemoteSession>> GetSessionsAsync(string accessToken, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<RemoteSession>>([]);

    public Task RevokeSessionAsync(string accessToken, string sessionId, CancellationToken ct)
        => Task.CompletedTask;
}
```

`IAuthGateway` members follow the SecureSession README. Do not invent a second refresh loop.

Pick **one** 401 path:

| Host | Token handler | ApiResilience `TokenRefresh` |
| --- | --- | --- |
| Android / iOS with SecureSession | `.AddSecureSession()` | Off |
| Any HttpForge TFM without SecureSession | `IAccessTokenProvider` | On |
| Already on MSAL / Auth0 | That SDK’s handler | Off |

Do not stack `.AddSecureSession()` and ApiResilience token refresh on the same client.

`GetAccessTokenAsync()` also refreshes proactively inside the skew window. AppLock locks the UI. SecureSession locks tokens. They are not substitutes.

---

## SmartUpload — resumable files

[Plugin.Maui.SmartUpload](https://github.com/nuvyntralabs/Plugin.Maui.SmartUpload) owns chunked upload, retry, and process-death resume. HttpForge `[Multipart]` / `StreamPart` is a single POST.

Use HttpForge for the JSON API around the file (create asset, confirm completion). Use SmartUpload for the bytes when the file must survive a kill.

```csharp
using Plugin.Maui.HttpForge;
using Plugin.Maui.SmartUpload;

builder
    .UseMauiApp<App>()
    .UseHttpForge()
    .UseSmartUpload(options =>
    {
        options.RequireHttps = true;
        options.ResumeInterruptedOnStart = true;
        options.DefaultChunkSize = 512 * 1024;
    });

builder.Services.AddHttpForgeClient<IMediaApi>(client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
});
```

```csharp
public interface IMediaApi
{
    [Post("/assets/{id}/complete")]
    Task Confirm(string id, CancellationToken cancellationToken = default);
}

var session = await uploads.EnqueueAsync(new UploadRequest
{
    FilePath = photoPath,
    Endpoint = new Uri("https://api.example.com/uploads"),
    Protocol = UploadProtocolKind.Tus,
    Headers =
    {
        ["Authorization"] = $"Bearer {accessToken}"
    }
});

uploads.SessionCompleted += async (_, e) =>
{
    await mediaApi.Confirm(e.Session.SessionId);
};
```

Do not set `RequireHttps = false` unless the host explicitly asked for `http://`.

---

## Suggested registration order

```csharp
builder
    .UseMauiApp<App>()
    .UseHttpForge()
    .UseSecureSession(...)    // optional — Android / iOS token store
    .UseApiResilience(...)    // optional — retry / circuit / offline queue
    .UseApiCache(...)         // optional — GET cache defaults
    .UseSmartUpload(...);     // optional — resumable bytes

builder.Services
    .AddHttpForgeClient<IAuthApi>(c => c.BaseAddress = new Uri("https://api.example.com"));

builder.Services
    .AddHttpForgeClient<IUserApi>(c => c.BaseAddress = new Uri("https://api.example.com"))
    .AddSecureSession()       // or skip and enable ApiResilience TokenRefresh
    .AddApiResilience()
    .AddApiCache();
```

---

## What not to wire

| Temptation | Why not |
| --- | --- |
| Retry attributes on the HttpForge interface | Resilience belongs on the handler pipeline |
| `IApiCache.GetAsync` around an HttpForge GET | Double-caches when `.AddApiCache()` is already on the client |
| `.AddSecureSession()` and ApiResilience token refresh together | Two 401 refresh loops |
| 401 refresh inside HttpForge | Use `AuthorizationHeaderValueGetter` only to attach a token; refresh with SecureSession or ApiResilience |
| `[Multipart]` for multi-megabyte resume | Use SmartUpload |
| SecureSession on Mac Catalyst / Windows | That plugin is Android + iOS |
| Observability just to “see HTTP” | Use `ILogger` or Diagnostics breadcrumbs if you already have them |
| Package reference from HttpForge → those siblings | Keeps the REST client usable without the suite |

---

## Related

- HttpForge README — contract
- [Docs/refit-comparison.md](refit-comparison.md) — HttpForge 1.1.0 vs Refit 15
- [Docs/roadmap.md](roadmap.md) — shipped 1.1 surface; reflection fallback is not planned
- [Plugin.Maui.ApiResilience](https://github.com/nuvyntralabs/Plugin.Maui.ApiResilience)
- [Plugin.Maui.ApiCache](https://github.com/nuvyntralabs/Plugin.Maui.ApiCache)
- [Plugin.Maui.SecureSession](https://github.com/nuvyntralabs/Plugin.Maui.SecureSession)
- [Plugin.Maui.SmartUpload](https://github.com/nuvyntralabs/Plugin.Maui.SmartUpload)
- Hub [architecture](https://github.com/nuvyntralabs/MauiEssentials/blob/main/docs/architecture.md) — how the catalog composes
