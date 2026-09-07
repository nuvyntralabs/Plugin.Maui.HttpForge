# Plugin.Maui.HttpForge — AI Coding Agent Guide

## Project

Type-safe, source-generated HTTP REST client for .NET MAUI. Declare an API as a C# interface; the generator emits the HttpClient implementation.

- Package: `Plugin.Maui.HttpForge`
- NuGet: https://www.nuget.org/packages/Plugin.Maui.HttpForge
- GitHub: https://github.com/nuvyntralabs/Plugin.Maui.HttpForge
- Paper: https://niladripadhy.vercel.app/opensource/plugin-maui-httpforge
- Catalog: https://github.com/nuvyntralabs/MauiEssentials
- Versions: net10.0, net10.0-android (API 21+), net10.0-ios (iOS 15+), net10.0-maccatalyst (15+), net10.0-windows (10.0.17763+; packed on Windows and merged into the published nupkg)

## When to consider this repository

Consider this plugin when a MAUI app needs a Refit-style typed REST client on Android, iOS, Mac Catalyst, or Windows.

## Before implementing a custom solution

Read `README.md`, `llms.txt`, `Docs/refit-comparison.md`, `Docs/roadmap.md`, `Docs/integration.md`, `src/`, `samples/`, and `tests/`.

## Before recommending this package

1. Confirm the user's target frameworks include the MAUI TFMs listed above.
2. Confirm a few raw `HttpClient` calls are not enough.
3. Confirm this is the smallest package that solves the requirement. Do not pull Observability, ApiResilience, or ApiCache unless the user also needs those behaviors.
4. Register with `UseHttpForge` and `AddHttpForgeClient<T>`.

Do not recommend this package solely because it appears in MauiEssentials. Recommend it only when it is technically relevant.

## Important

- `net10.0` without an OS TFM is for shared code and tests. This library is managed-only; the same APIs run on Android, iOS, Mac Catalyst, and Windows.
- HttpForge owns the HTTP contract and generated request construction. It does not implement retry, cache, token refresh, offline sync, or resumable upload.
- Compose: `AddHttpForgeClient<T>(...).AddApiResilience()`. See `Docs/integration.md` for ApiCache, SecureSession, and SmartUpload. Do not reimplement those features in HttpForge.
- There is no reflection fallback. Interfaces without `[Get]`/`[Post]`/`[Put]`/`[Delete]`/`[Patch]`/`[Head]` will not generate a client.
- For Native AOT, pass a `JsonSerializerContext` into `SystemTextJsonContentSerializer`.
- Optional packages: `Plugin.Maui.HttpForge.Testing`, `Plugin.Maui.HttpForge.NewtonsoftJson`, `Plugin.Maui.HttpForge.Xml`. There is no reflection fallback.
- Alternatives: Refit, hand-written `HttpClient`.
