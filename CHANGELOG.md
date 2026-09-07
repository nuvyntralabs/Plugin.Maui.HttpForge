# Changelog

## Unreleased

## 1.1.0

- Query objects, collection formats (`Multi` / `Csv` / `Ssv` / `Tsv` / `Pipes`), and camel/snake/kebab key formatters
- `[Timeout]`, `[Url]`, `[PathPrefix]`, and optional route segments (`{id?}`)
- `[QueryName]` valueless flags and `[FormObject]` multipart flattening
- `IAsyncEnumerable<T>` streaming (JSON Lines and SSE) and `[Body(BodySerializationMethod.JsonLines)]`
- Request-body gzip/brotli (`RequestBodyCompression` / `[CompressRequest]`)
- `AuthorizationHeaderValueGetter` (token attach only — refresh stays in SecureSession / ApiResilience). The getter receives an absolute URI (BaseAddress + relative path).
- Optional packages: `Plugin.Maui.HttpForge.Testing`, `Plugin.Maui.HttpForge.NewtonsoftJson`, `Plugin.Maui.HttpForge.Xml`
- Reflection fallback remains out of scope (generated-only)
- Docs: README / llms.txt 1.1 surface, [Docs/refit-comparison.md](Docs/refit-comparison.md) vs Refit 15

## 1.0.1

- Merge the Windows CI nupkg into the published package so NuGet.org lists a real `net10.0-windows` TFM (not a compatibility hint from `net10.0`).
- Document the post-1.0.0 surface in `Docs/roadmap.md` (query objects, timeouts, streaming, testing package).
- Document sibling composition in `Docs/integration.md` (ApiResilience, ApiCache, SecureSession, SmartUpload).

## 1.0.0

- Source-generated REST client for GET, POST, PUT, DELETE, PATCH, and HEAD
- Path parameters, query parameters, JSON bodies, static and dynamic headers
- Multipart uploads (`StreamPart`, `ByteArrayPart`, `FileInfoPart`)
- `Task<T>`, `Task`, `Task<IApiResponse<T>>`, and `Task<HttpResponseMessage>`
- `ApiException` vs `ApiRequestException`
- `UseHttpForge` / `AddHttpForgeClient<T>` for MAUI DI and `IHttpClientFactory`
- Compile-time diagnostics (HFG001–HFG006)
- .NET MAUI support for Android, iOS, Mac Catalyst, and Windows
