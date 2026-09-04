# Changelog

## 1.0.0

- Source-generated REST client for GET, POST, PUT, DELETE, PATCH, and HEAD
- Path parameters, query parameters, JSON bodies, static and dynamic headers
- Multipart uploads (`StreamPart`, `ByteArrayPart`, `FileInfoPart`)
- `Task<T>`, `Task`, `Task<IApiResponse<T>>`, and `Task<HttpResponseMessage>`
- `ApiException` vs `ApiRequestException`
- `UseHttpForge` / `AddHttpForgeClient<T>` for MAUI DI and `IHttpClientFactory`
- Compile-time diagnostics (HFG001–HFG006)
- .NET MAUI support for Android, iOS, Mac Catalyst, and Windows
