# Changelog

## Unreleased

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
