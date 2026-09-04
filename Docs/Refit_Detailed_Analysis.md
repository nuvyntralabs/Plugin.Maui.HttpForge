# Refit Library — Detailed Architecture, Feature, Source-Code and Design Analysis

**Research date:** 4 September 2026  
**Analyzed baseline:** Refit 15.2.0 / current `main` documentation and source  
**Repository:** https://github.com/reactiveui/refit  
**Documentation:** https://reactiveui.github.io/refit/  
**License:** MIT

---

## 1. Executive Summary

Refit is a type-safe REST API client library for .NET. Its central idea is simple: developers declare an HTTP API as a C# interface decorated with attributes, and Refit generates an implementation that uses `HttpClient`.

Example:

```csharp
public interface IUserApi
{
    [Get("/users/{id}")]
    Task<User> GetUser(int id);

    [Post("/users")]
    Task<User> CreateUser([Body] CreateUserRequest request);
}
```

The important architectural evolution of Refit is that it has moved from a primarily runtime/reflection-driven implementation toward **Roslyn source-generated clients and source-generated request construction**.

The modern pipeline is:

```text
C# API Interface
       |
       v
Refit Attributes
       |
       v
Roslyn Source Generator
       |
       +----------------------+
       |                      |
       v                      v
Generated Client       Generated Request Builder
       |                      |
       +----------+-----------+
                  |
                  v
              HttpClient
                  |
                  v
        DelegatingHandler chain
                  |
                  v
              HTTP API
```

This evolution is particularly important for:

- .NET MAUI
- trimming
- Native AOT
- startup performance
- runtime allocations
- compile-time diagnostics
- predictable generated code

Refit 14 completed the major move toward reflection-free generated request building. The old reflection request builder became an opt-in `Refit.Reflection` package. Refit 15 continues the optimization and maintenance work; Refit 15.2.0 added request-body compression and additional generator/debugging/header fixes.

At the time of this analysis, NuGet lists Refit 15.2.0 as the latest stable version, with more than 190 million total downloads for the Refit package. Refit.HttpClientFactory is also above 117 million total downloads. These numbers indicate a mature and widely adopted ecosystem.

---

# 2. What Problem Refit Solves

Without Refit, a typical REST client contains repetitive code:

```csharp
var response = await httpClient.GetAsync($"users/{id}");

response.EnsureSuccessStatusCode();

var json = await response.Content.ReadAsStringAsync();

var user = JsonSerializer.Deserialize<User>(json);
```

With Refit:

```csharp
var user = await api.GetUser(id);
```

The developer moves the HTTP contract into an interface:

```csharp
public interface IUserApi
{
    [Get("/users/{id}")]
    Task<User> GetUser(int id);
}
```

The interface becomes a declarative description of:

- HTTP method
- route
- path parameters
- query parameters
- headers
- request body
- multipart content
- return type
- cancellation
- response handling

The implementation is generated rather than manually written.

---

# 3. Core Design Philosophy

Refit's design can be summarized as:

> **Declare the API contract; let the library generate the HTTP plumbing.**

It deliberately does not attempt to replace the complete .NET HTTP stack.

Refit relies heavily on:

- `HttpClient`
- `HttpClientFactory`
- `HttpRequestMessage`
- `HttpResponseMessage`
- `DelegatingHandler`
- dependency injection
- `System.Text.Json`
- Roslyn source generators

This is an important architectural decision.

Refit is therefore not a networking stack. It is an **API-client generation layer over the standard .NET HTTP stack**.

---

# 4. High-Level Architecture

```text
                         Application
                              |
                              v
                    IUserApi / IOrderApi
                              |
                     Refit-generated code
                              |
                  +-----------+-----------+
                  |                       |
          Request generation       Response processing
                  |                       |
                  v                       v
          HttpRequestMessage       Deserialize response
                  |                       |
                  +-----------+-----------+
                              |
                           HttpClient
                              |
                    IHttpMessageHandler
                              |
              +---------------+----------------+
              |               |                |
          Auth Handler    Logging Handler   Retry Handler
              |               |                |
              +---------------+----------------+
                              |
                              v
                         HTTP Server
```

The generated client is responsible for translating the interface call into an HTTP operation.

The underlying `HttpClient` remains responsible for transport.

This separation is one of Refit's strongest architectural characteristics.

---

# 5. Source Tree / Repository Organization

The repository is organized around several major areas.

Conceptually:

```text
refit/
|
+-- src/
|   |
|   +-- Refit/
|   |    Runtime/core library
|   |
|   +-- Refit.HttpClientFactory/
|   |    Microsoft DI + IHttpClientFactory integration
|   |
|   +-- Refit.Newtonsoft.Json/
|   |    Newtonsoft.Json serializer integration
|   |
|   +-- Refit.Xml/
|   |    XML serializer integration
|   |
|   +-- Refit.Reflection/
|   |    Optional legacy/runtime reflection request builder
|   |
|   +-- Refit.Testing/
|        First-party client testing infrastructure
|   |
|   +-- InterfaceStubGenerator.*
|        Roslyn source-generator implementation
|   |
|   +-- tests/
|   |
|   +-- benchmarks/
|   |
|   +-- examples/
|
+-- docs/
|
+-- README.md
|
+-- Directory.Build.*
|
+-- Refit.slnx
```

The repository's own contributor documentation identifies `src/InterfaceStubGenerator.*` as the source-generator area and `src/Refit.slnx` as the main solution.

---

# 6. Package Architecture

The ecosystem is deliberately split into packages.

## 6.1 Refit

Core package.

Responsibilities include:

- Refit attributes
- generated client infrastructure
- request/response abstractions
- serialization abstraction
- API response types
- exceptions
- settings
- URL formatting
- runtime helpers required by generated clients

Current stable baseline:

```text
Refit 15.2.0
```

---

## 6.2 Refit.HttpClientFactory

Provides integration with:

```csharp
IServiceCollection
IHttpClientFactory
IHttpClientBuilder
```

Typical usage:

```csharp
services
    .AddRefitClient<IUserApi>()
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri("https://api.example.com");
    });
```

It enables:

- dependency injection
- named/typed HTTP clients
- DelegatingHandlers
- handler lifetime configuration
- resilience pipelines
- authentication handlers
- logging
- standard Microsoft HTTP infrastructure

---

## 6.3 Refit.Newtonsoft.Json

Provides a Newtonsoft.Json serializer integration for applications that require:

- existing Json.NET converters
- Json.NET-specific behavior
- legacy serialization compatibility
- Json.NET settings

System.Text.Json remains the default serializer.

---

## 6.4 Refit.Xml

XML serialization is separated into its own package.

This keeps XML functionality optional rather than forcing all consumers to carry the XML serializer dependency.

---

## 6.5 Refit.Reflection

This is especially important from an architectural perspective.

Before the source-generation transition, Refit's runtime reflection request builder was part of the normal implementation.

In Refit 14, the reflection request builder became an opt-in package.

This means:

```text
Normal modern application
        |
        v
Generated request builder
        |
        v
No reflection request-building path
```

Whereas compatibility scenarios can explicitly use:

```text
Refit
 +
Refit.Reflection
        |
        v
Runtime reflection request builder
```

This is useful for unusual API shapes or environments that cannot use the source generator.

---

## 6.6 Refit.Testing

First-party testing support.

It allows a real Refit client to execute against a Refit-aware stub HTTP handler.

Important capabilities:

- declarative routes
- typed replies
- request verification
- typed request-body inspection
- network latency simulation
- fault injection
- retry testing
- timeout testing
- `IApiResponse<T>` consumer testing

This is much more specialized than a generic `HttpMessageHandler` mock.

---

# 7. API Declaration Model

Refit APIs are described using attributes.

The basic HTTP attributes are:

```text
[Get]
[Post]
[Put]
[Delete]
[Patch]
[Head]
```

Example:

```csharp
public interface IProductApi
{
    [Get("/products")]
    Task<List<Product>> GetProducts();

    [Get("/products/{id}")]
    Task<Product> GetProduct(int id);

    [Post("/products")]
    Task<Product> CreateProduct([Body] CreateProductRequest request);

    [Put("/products/{id}")]
    Task<Product> UpdateProduct(
        int id,
        [Body] UpdateProductRequest request);

    [Delete("/products/{id}")]
    Task DeleteProduct(int id);
}
```

The interface is effectively an API DSL embedded in C#.

---

# 8. Route Processing

A route can contain:

```text
/users/{id}
/users/{id}/orders/{orderId}
/companies/{companyId}/employees
```

Parameters are substituted into the URL.

Example:

```csharp
[Get("/users/{id}")]
Task<User> GetUser(int id);
```

Calling:

```csharp
await api.GetUser(42);
```

produces conceptually:

```text
GET /users/42
```

Refit also supports aliases:

```csharp
[Get("/users/{userId}")]
Task<User> GetUser([AliasAs("userId")] int id);
```

---

# 9. URL and Path Features

Modern Refit includes a broad set of URL features.

Important capabilities include:

- path parameters
- `[AliasAs]`
- `[Url]`
- optional route segments
- `[PathPrefix]`
- URL parameter formatting
- URL parameter key formatting
- custom formatters
- encoded values
- absolute per-call URLs

Example:

```csharp
[Get("/users/{id}")]
Task<User> GetUser(int id);
```

For APIs with unusual URL requirements, the customization layer is significantly more powerful than simple string interpolation.

---

# 10. Query Parameters

Simple parameters:

```csharp
[Get("/users")]
Task<List<User>> Search(string name, int page);
```

Conceptually:

```text
/users?name=John&page=1
```

---

# 11. Query Object

Refit supports complex query objects.

```csharp
public class UserQuery
{
    public string? Name { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}

[Get("/users")]
Task<List<User>> Search([Query] UserQuery query);
```

This can produce:

```text
/users?Name=John&Page=1&PageSize=20
```

Naming can be customized.

---

# 12. Query Naming

Refit supports:

```text
[AliasAs]
UrlParameterKeyFormatter
```

Built-in naming conventions include:

```text
CamelCase
SnakeCase
KebabCase
```

For example:

```csharp
RefitSettings.SnakeCase()
```

can provide consistent naming across relevant serialized/query values.

This is a good example of a library-level convenience that removes repetitive API mapping code.

---

# 13. Collection Query Parameters

Refit supports different collection formats.

For example:

```csharp
[Get("/users")]
Task<List<User>> Search(
    [Query(CollectionFormat.Multi)]
    int[] ages);
```

can generate:

```text
/users?ages=10&ages=20&ages=30
```

CSV format:

```text
/users?ages=10,20,30
```

Indexed/deep-object style is also supported:

```text
items[0].ProductId=1
items[0].Quantity=2
items[1].ProductId=5
items[1].Quantity=1
```

This is particularly useful for OpenAPI-style APIs.

---

# 14. Valueless Query Flags

Some APIs require:

```text
/items?archived
```

rather than:

```text
/items?archived=true
```

Refit supports this through:

```csharp
[QueryName]
```

Example:

```csharp
[Get("/items")]
Task<List<Item>> List([QueryName] string flag);
```

---

# 15. Request Bodies

Refit supports several body modes.

## JSON

```csharp
[Post("/users")]
Task<User> Create([Body] User user);
```

## String

```csharp
[Post("/data")]
Task Send([Body] string data);
```

## URL encoded

```csharp
[Post("/login")]
Task Login(
    [Body(BodySerializationMethod.UrlEncoded)]
    LoginRequest request);
```

## JSON Lines

```csharp
[Post("/batch")]
Task Send(
    [Body(BodySerializationMethod.JsonLines)]
    IEnumerable<Item> items);
```

## Stream

```csharp
[Post("/upload")]
Task Upload([Body] Stream stream);
```

---

# 16. Request Body Buffering

Refit can stream request bodies instead of buffering them.

This matters for:

- large files
- large JSON payloads
- memory-constrained mobile devices

Example:

```csharp
[Body(buffered: true)]
```

Buffering provides a `Content-Length`, while streaming can avoid loading the complete content into memory.

This is especially relevant for .NET MAUI applications.

---

# 17. Multipart

Refit supports multipart uploads.

Typical parts include:

```text
StreamPart
ByteArrayPart
FileInfoPart
MultipartItem
Stream
FileInfo
byte[]
HttpContent
```

Example:

```csharp
[Multipart]
[Post("/upload")]
Task Upload(
    [AliasAs("file")]
    StreamPart file);
```

This is useful for:

- image uploads
- document uploads
- profile pictures
- media uploads
- mobile capture/upload scenarios

---

# 18. Form Objects

Modern Refit supports flattening objects into multipart/form fields using `[FormObject]`.

This is useful when the API expects:

```text
name=John
address.city=Hyderabad
address.zip=500001
```

rather than a JSON body.

---

# 19. Headers

Static headers:

```csharp
[Headers("User-Agent: MyApp")]
```

Dynamic headers:

```csharp
Task<User> GetUser(
    [Header("Authorization")] string token);
```

Header collection:

```csharp
Task<User> GetUser(
    [HeaderCollection]
    IDictionary<string, string> headers);
```

This supports APIs requiring:

- Authorization
- API keys
- tenant IDs
- correlation IDs
- custom feature headers

---

# 20. Authentication

Refit does not try to become an authentication framework.

Instead it provides integration points.

A common pattern is:

```csharp
[Headers("Authorization: Bearer")]
```

combined with:

```csharp
AuthorizationHeaderValueGetter
```

The token provider can be responsible for:

- token retrieval
- caching
- refresh
- expiration

This is architecturally preferable to putting token logic in every API method.

---

# 21. DelegatingHandlers

One of the most important extension points is the standard .NET `DelegatingHandler`.

Pipeline:

```text
Refit
  |
  v
AuthenticationHandler
  |
  v
LoggingHandler
  |
  v
RetryHandler
  |
  v
HttpClient
```

This enables clean separation of cross-cutting concerns.

Examples:

```text
Authentication
Retry
Logging
Telemetry
Caching
Compression
Correlation IDs
Request signing
Offline detection
```

This is one reason Refit integrates well with enterprise .NET applications.

---

# 22. Passing State to DelegatingHandlers

Modern Refit exposes information that can be useful to handlers, including:

- call arguments
- target interface type
- invoked method metadata

This enables more advanced policies.

Example use cases:

```text
Different retry policy per API method
Different authentication policy
Per-endpoint telemetry
Endpoint-specific logging
Request prioritization
```

---

# 23. Response Types

The simplest form is:

```csharp
Task<User>
```

For richer response handling, Refit supports:

```csharp
Task<IApiResponse<User>>
Task<ApiResponse<User>>
```

These provide access to information such as:

```text
StatusCode
Headers
Content
Error
IsSuccessStatusCode
```

This is useful when the application needs to inspect HTTP semantics instead of treating every non-success result as an exception.

---

# 24. Exception Model

Refit provides several exception concepts.

Important ones include:

```text
ApiException
ApiExceptionBase
ApiRequestException
```

The architecture distinguishes between:

```text
Server/API response failures
        vs
Transport/request failures
```

This became particularly important around Refit 11/12.

A useful conceptual hierarchy is:

```text
ApiExceptionBase
|
+-- ApiException
|     |
|     +-- Server returned an HTTP response
|
+-- ApiRequestException
      |
      +-- Transport/request-level failure
```

Applications should therefore not assume every failure is simply a server status code.

---

# 25. ExceptionFactory

Refit provides:

```text
ExceptionFactory
```

This can customize how API response errors are turned into exceptions.

Use cases:

- custom error models
- suppressing exceptions
- mapping errors
- integrating domain-specific exceptions
- logging/telemetry

---

# 26. TransportExceptionFactory

Transport failures are different from HTTP API failures.

Examples:

```text
DNS failure
connection refused
network unavailable
TLS failure
socket failure
timeout
```

Modern Refit provides:

```text
TransportExceptionFactory
```

for customizing this layer.

This is highly relevant to mobile applications because network availability is much less deterministic on mobile devices.

---

# 27. Cancellation

Refit supports `CancellationToken`.

Example:

```csharp
[Get("/users")]
Task<List<User>> GetUsers(
    CancellationToken cancellationToken);
```

This allows MAUI applications to cancel requests when:

- a page disappears
- a search operation becomes obsolete
- the user navigates away
- a ViewModel is disposed
- a timeout occurs

---

# 28. Per-Method Timeout

Modern Refit supports a `[Timeout]` attribute.

This provides endpoint-level timeout control.

Conceptually:

```csharp
[Get("/slow")]
[Timeout(5000)]
Task<Response> GetSlowEndpoint();
```

This is useful when different APIs have different expected latency.

However, application-wide `HttpClient`/resilience policies should still be considered for broader policy control.

---

# 29. Streaming Responses

Refit supports:

```csharp
IAsyncEnumerable<T>
```

This enables streamed response processing.

Modern support includes:

- JSON arrays/streaming
- JSON Lines
- Server-Sent Events

SSE responses can be consumed as:

```text
IAsyncEnumerable<T>
```

This is useful for:

- live updates
- event feeds
- telemetry
- long-running server streams

---

# 30. Building HttpRequestMessage Without Sending

Modern Refit supports an API method returning:

```csharp
Task<HttpRequestMessage>
```

This builds the request without dispatching it.

Useful scenarios:

- request inspection
- signing
- custom dispatch
- debugging
- custom transport
- request auditing

This is an excellent example of Refit exposing an escape hatch without abandoning its declarative model.

---

# 31. Serialization Architecture

Refit abstracts serialization through an HTTP-content serializer interface.

Default:

```text
System.Text.Json
```

Optional:

```text
Newtonsoft.Json
XML
Custom serializer
```

This allows:

```text
API interface
     |
     v
Refit
     |
     v
IHttpContentSerializer
     |
     +---- System.Text.Json
     |
     +---- Newtonsoft.Json
     |
     +---- XML
     |
     +---- Custom
```

---

# 32. System.Text.Json

System.Text.Json is the default serializer.

This is important because it aligns Refit with modern .NET.

Advantages include:

- good performance
- low allocations
- built-in framework integration
- source-generation support
- trimming/AOT compatibility

Refit also provides mechanisms for integrating `JsonSerializerContext`.

---

# 33. JSON Source Generation

There are two separate source-generation concepts:

```text
Refit source generator
        +
System.Text.Json source generator
```

They solve different problems.

### Refit generator

Generates:

```text
API client
HTTP request construction
```

### JSON generator

Generates:

```text
JSON serialization metadata
```

Combined:

```text
API interface
    |
    v
Refit generator
    |
    v
HttpRequestMessage
    |
    v
System.Text.Json generated metadata
    |
    v
JSON
```

This is highly relevant for Native AOT.

---

# 34. Fast-Path JSON Serialization

Refit documentation describes an optional fast-path configuration using:

```csharp
JsonSerializerContext
```

and appropriate `JsonSerializerOptions`.

This can avoid slower metadata-based serialization in supported scenarios.

However, there are trade-offs involving:

- converters
- polymorphism
- `object` values
- naming
- synchronous versus asynchronous serialization APIs

Therefore this should be treated as an optimization rather than a default requirement.

---

# 35. Native AOT and Trimming

This is one of Refit's most important modern architectural areas.

Historically:

```text
Runtime reflection
        |
        v
Trim/AOT risk
```

Modern Refit:

```text
Compile time
    |
    v
Generated implementation
    |
    v
Generated request construction
    |
    v
Runtime
```

Refit 14 specifically completed the move so that fully generated interfaces do not need the reflection request builder.

For Native AOT / trimmed applications, Refit recommends source-generated clients and provides:

```csharp
RestService.ForGenerated<T>()
```

and:

```csharp
AddRefitGeneratedClient<T>()
```

The latter is especially important for DI because it avoids the reflection fallback and its associated trimming/dynamic-code requirements.

---

# 36. Source Generator Architecture

The source generator is one of the most technically interesting parts of the repository.

Conceptual pipeline:

```text
Compilation
    |
    v
Find Refit interfaces
    |
    v
Read attributes
    |
    v
Parse methods
    |
    v
Build semantic model
    |
    v
Validate API definition
    |
    v
Generate client implementation
    |
    v
Generate request-building code
```

The repository uses incremental Roslyn-generator techniques.

The contributor documentation specifically mentions:

- `ForAttributeWithMetadataName`
- value-equatable record-struct models
- immutable equatable arrays
- pooled string builders
- incremental-cache regression tests
- BenchmarkDotNet generator benchmarks

This indicates that generator performance is treated as a first-class concern.

---

# 37. Generated Client vs Generated Request Builder

These are worth separating.

Older source generation could generate the client while still calling a runtime request builder.

Modern Refit can generate both:

```text
Generated interface implementation
+
Generated HTTP request construction
```

So instead of:

```text
Generated Client
      |
      v
Reflection RequestBuilder
      |
      v
HttpRequestMessage
```

modern generated methods can effectively do:

```text
Generated Client
      |
      v
Create HttpRequestMessage directly
      |
      v
HttpClient
```

This eliminates:

- runtime reflection metadata lookup
- descriptor creation
- argument boxing in many cases
- delegate construction
- runtime request-shaping work

---

# 38. Reflection Fallback

Not every possible request shape can necessarily be emitted inline.

For unsupported or unusual shapes, Refit can fall back to the reflection request builder if `Refit.Reflection` is included.

This creates an important compatibility strategy:

```text
Common API shape
      |
      v
Generated path
```

and:

```text
Unusual API shape
      |
      v
Reflection fallback
```

For maximum AOT safety:

```text
Use generated-only APIs
+
avoid unsupported shapes
+
do not include reflection fallback
```

---

# 39. Analyzer Architecture

Refit ships analyzers with the package.

Examples of diagnostics include:

- methods/properties that cannot be generated
- malformed routes
- multiple `CancellationToken` parameters
- invalid `HeaderCollection`
- multiple `[Body]` parameters
- `[Multipart]` combined incorrectly with `[Body]`

This is a major improvement over discovering configuration errors at runtime.

The compiler becomes part of the API validation pipeline.

---

# 40. Compile-Time Safety Model

The Refit interface acts as a contract.

Example:

```csharp
[Get("/users/{id}")]
Task<User> GetUser(int id);
```

The compiler knows:

```text
method exists
parameter type = int
return type = Task<User>
```

The generator additionally validates:

```text
route
attributes
parameter binding
body configuration
headers
cancellation
response type
```

This gives Refit two levels of safety:

```text
C# type system
       +
Refit analyzer/generator validation
```

---

# 41. HttpClientFactory Integration

The recommended enterprise-style architecture is:

```text
DI container
    |
    v
IHttpClientFactory
    |
    v
HttpClient
    |
    v
Refit-generated client
```

Example:

```csharp
services
    .AddRefitClient<IUserApi>()
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress =
            new Uri("https://api.example.com");
    });
```

Then:

```csharp
public class UserService
{
    private readonly IUserApi _api;

    public UserService(IUserApi api)
    {
        _api = api;
    }
}
```

---

# 42. HttpClient Naming

Refit creates deterministic names for clients.

It also allows explicitly providing a human-readable HTTP client name.

This is useful for:

- logging categories
- diagnostics
- direct `IHttpClientFactory` configuration
- large applications containing many API clients

---

# 43. Keyed Client Registration

Modern Refit supports keyed client registration.

This enables multiple configurations of the same API interface.

Conceptually:

```text
IUserApi + Tenant A
IUserApi + Tenant B
```

with separate configuration keys.

This is useful for:

- multi-tenant systems
- multiple API hosts
- regional APIs
- staging/production variants
- multiple credentials

---

# 44. Resilience Integration

Refit intentionally leaves resilience to the underlying HTTP pipeline.

That means Refit can work with:

- DelegatingHandlers
- Polly
- Microsoft.Extensions.Http.Resilience
- custom handlers

Recommended architecture:

```text
Refit
  |
  v
HttpClient
  |
  v
Resilience pipeline
  |
  +-- Timeout
  +-- Retry
  +-- Circuit breaker
  +-- Rate limiter
  |
  v
Network
```

This is better than embedding retry logic directly into every Refit API interface.

---

# 45. Refit.Testing Architecture

`Refit.Testing` introduces a Refit-aware stub handler.

Conceptually:

```text
Test
 |
 v
Refit API client
 |
 v
StubHttp
 |
 +--> Route table
 |
 +--> Reply
 |
 +--> Request capture
 |
 +--> Network simulation
```

Example:

```csharp
var http = new StubHttp
{
    {
        Route.Get("/users/{id}"),
        Reply.With(new User(7, "octocat"))
    }
};

var api = http.CreateClient<IGitHubApi>(
    "https://api.github.com");

var user = await api.GetUser(7);

await http.VerifyAllCalledAsync();
```

This has an important advantage over generic HTTP mocking: the test uses Refit concepts.

---

# 46. Testing Network Conditions

`Refit.Testing` supports network behavior simulation.

Possible scenarios include:

```text
Latency
Timeout
Transport failure
Retry
Transient fault
Repeated request
```

This is especially useful for mobile application testing because network reliability is inherently variable.

---

# 47. Interface Inheritance

Refit supports interface inheritance.

Example:

```csharp
public interface IBaseApi
{
    [Get("/health")]
    Task<Health> Health();
}

public interface IUserApi : IBaseApi
{
    [Get("/users")]
    Task<List<User>> GetUsers();
}
```

This enables shared API contracts.

---

# 48. API Composition

Multiple API interfaces can be composed into a larger client abstraction.

This allows teams to split:

```text
IUserApi
IOrderApi
IProductApi
IPaymentApi
```

instead of creating one giant interface.

A clean architecture could be:

```text
API Layer
|
+-- IUserApi
+-- IOrderApi
+-- IProductApi
+-- IPaymentApi
```

---

# 49. Generic Interfaces

Refit supports generic API interfaces.

This enables reusable API contracts.

However, generic API designs should be used carefully. Over-generalizing REST APIs can hide endpoint-specific semantics and make generated code harder to understand.

---

# 50. Default Interface Methods

Default interface methods allow logic to exist alongside declarative HTTP methods.

Example:

```csharp
public interface IUserApi
{
    [Get("/users")]
    Task<List<User>> GetUsersInternal();

    async Task<List<User>> GetActiveUsers()
    {
        var users = await GetUsersInternal();

        return users
            .Where(x => x.IsActive)
            .ToList();
    }
}
```

This can be useful for small API-client convenience methods.

However, domain/business logic should generally remain outside the API contract.

---

# 51. Request Properties

Refit provides mechanisms for passing state/properties through the request pipeline.

This can be used by handlers for:

- diagnostics
- endpoint metadata
- custom routing decisions
- correlation
- policy selection

It is another example of Refit extending the standard `HttpClient` pipeline instead of replacing it.

---

# 52. API Versioning

Refit does not impose an API-versioning strategy.

You can implement versioning through:

```text
BaseAddress
Routes
Headers
Query parameters
DelegatingHandlers
```

Example:

```text
/api/v1/users
/api/v2/users
```

or:

```text
Accept: application/vnd.company.v2+json
```

The absence of a hard-coded versioning abstraction keeps Refit flexible.

---

# 53. Request Compression

Refit 15.2.0 added request-body compression.

This is particularly relevant for:

- mobile networks
- large JSON payloads
- synchronization APIs
- bandwidth-sensitive applications

It is an example of Refit continuing to add transport-adjacent conveniences while still relying on `HttpClient`.

---

# 54. Security Evolution

Refit 13 introduced significant security hardening following an audit.

Notable changes included:

- XML deserialization protection against XXE
- safer Newtonsoft.Json type handling defaults
- redaction of sensitive authentication values from exceptions/log output

This demonstrates an important lesson for library authors:

> Serialization defaults are security decisions, not merely convenience settings.

---

# 55. Refit Version Evolution

## Refit 6

Major modernization:

- System.Text.Json became default
- Newtonsoft.Json became optional
- XML moved to `Refit.Xml`
- analyzer/source-generator-era packaging requirements

---

## Refit 12

Major request-generation rewrite:

- source-generated request construction
- reflection path retained as fallback
- streaming support
- JSON Lines
- naming presets
- response API changes

---

## Refit 13

Security and testing focus:

- security hardening
- first-party `Refit.Testing`
- improved generated path parameters

---

## Refit 14

Major architectural milestone:

- generated request building became the primary path
- reflection request builder moved to `Refit.Reflection`
- Native AOT/trimming story significantly improved
- `[Url]`
- `[PathPrefix]`
- `[Timeout]`
- `[FormObject]`
- optional route segments
- SSE streaming
- request creation without sending
- improved analyzers
- performance improvements

---

## Refit 15

Continues optimization and compatibility work.

The 15.x series introduced/continued:

- cancellation behavior improvements
- generator/tooling fixes
- performance work
- analyzer packaging improvements
- request-body compression in 15.2.0

---

# 56. Current Package Baseline

At the research date, NuGet identifies:

```text
Refit                    15.2.0
Refit.HttpClientFactory  15.2.0
Refit.Newtonsoft.Json    15.2.0
Refit.Xml                15.2.0
Refit.Reflection         15.2.0
Refit.Testing            15.2.0
```

The package ecosystem is substantial.

Refit itself has over 190 million downloads according to NuGet's current package page, while Refit.HttpClientFactory is above 117 million.

These figures should be interpreted as ecosystem adoption indicators rather than unique users.

---

# 57. Target Frameworks

Current Refit documentation lists support for modern platforms/targets including:

- .NET 8
- .NET 9
- .NET 10
- .NET 11
- WinUI
- Blazor
- Uno Platform
- .NET Framework 4.6.2+

The exact target framework matrix should always be verified against the specific package version being consumed.

---

# 58. .NET MAUI Suitability

Refit is a strong fit for .NET MAUI because MAUI applications already use:

```text
Microsoft.Extensions.DependencyInjection
HttpClient
HttpClientFactory
System.Text.Json
CancellationToken
```

A recommended architecture is:

```text
MAUI
 |
 +-- ViewModel
 |
 +-- Application Service
 |
 +-- Refit Interface
 |
 +-- HttpClientFactory
 |
 +-- Auth Handler
 |
 +-- Resilience Handler
 |
 +-- HttpClient
 |
 +-- API
```

This keeps UI code independent of HTTP implementation details.

---

# 59. MAUI-Specific Benefits

## Reduced boilerplate

Instead of writing HTTP plumbing in ViewModels:

```csharp
await _httpClient.GetAsync(...);
```

use:

```csharp
await _userApi.GetUser(id);
```

## Cancellation

Navigation can cancel API calls.

## DI integration

API clients can be injected.

## Serialization

System.Text.Json integrates naturally with MAUI/.NET.

## Source generation

This improves the trimming/AOT story.

## Multipart

Useful for camera/document upload scenarios.

## Streaming

Useful for live API feeds.

---

# 60. MAUI-Specific Gaps

Refit is not a mobile networking framework.

It does not inherently solve:

- offline-first data
- connectivity state
- persistent request queue
- background synchronization
- mobile retry policy
- token storage
- token refresh persistence
- network reachability
- app lifecycle cancellation
- local cache
- stale-while-revalidate
- conflict resolution
- upload resume
- chunked resumable upload
- bandwidth-aware scheduling

These must be implemented around Refit.

---

# 61. Memory Characteristics

Refit's modern generated request construction can reduce several categories of runtime work:

```text
Reflection lookup
Metadata descriptors
Delegate construction
Argument boxing
Runtime request-shape analysis
```

However, Refit does not magically make HTTP allocation-free.

Typical allocations still include:

```text
HttpRequestMessage
HttpContent
HTTP buffers/streams
Serialization buffers
Response objects
Deserialized models
Exceptions on failures
```

The correct conclusion is:

> Refit can reduce API-client overhead, but network I/O and serialization remain dominant costs for most real-world API calls.

---

# 62. Performance Architecture

A simplified performance comparison:

### Older reflection-heavy approach

```text
Method call
  |
  v
Reflection
  |
  v
Inspect MethodInfo
  |
  v
Inspect parameters
  |
  v
Build descriptors
  |
  v
Build request
```

### Modern generated approach

```text
Method call
  |
  v
Generated method
  |
  v
Build request directly
```

This reduces runtime work.

The repository itself maintains runtime and generator benchmarks, rather than relying solely on theoretical analysis.

---

# 63. Source Generator Performance

The repository's contributor guidance shows deliberate attention to incremental generator performance.

Important techniques include:

- incremental caching
- value-equatable models
- immutable collections
- pooled string builders
- focused benchmarks
- EventPipe profiling

This is an excellent pattern for anyone designing a source-generator-based NuGet library.

---

# 64. Diagnostics Strategy

Refit's analyzer strategy demonstrates an important library design principle:

### Bad

```text
Compile
  |
  v
Run application
  |
  v
HTTP call
  |
  v
Runtime error
```

### Better

```text
Compile
  |
  v
Analyzer
  |
  v
Diagnostic
  |
  v
Developer fixes API declaration
```

This significantly improves developer experience.

---

# 65. Error Handling Strategy

There are three major error classes to think about:

```text
1. API/HTTP failure
       |
       +-- 400
       +-- 401
       +-- 403
       +-- 404
       +-- 500

2. Serialization failure
       |
       +-- malformed response
       +-- incompatible model

3. Transport failure
       |
       +-- DNS
       +-- TLS
       +-- socket
       +-- timeout
       +-- network unavailable
```

A robust application should not collapse these into one generic exception.

---

# 66. Refit Strengths

## 1. Excellent developer experience

```csharp
[Get("/users/{id}")]
Task<User> GetUser(int id);
```

is extremely productive.

## 2. Strong type safety

The interface is checked by C# and Refit analyzers.

## 3. Source generation

Modern Refit is much better suited to trimming/AOT than older versions.

## 4. Standard HttpClient integration

It works with the .NET ecosystem instead of creating a proprietary transport stack.

## 5. Extensibility

Handlers, serializers, settings and factories provide many extension points.

## 6. Mature ecosystem

Large NuGet adoption and a long project history reduce adoption risk.

## 7. Testing

First-party Refit-aware testing is a strong recent addition.

---

# 67. Refit Weaknesses

## 1. Attribute-heavy interfaces

Complex endpoints can become difficult to read.

Example:

```csharp
[Multipart]
[Headers(...)]
[Post(...)]
Task<ApiResponse<Result>> Upload(
    [AliasAs(...)]
    [Header(...)]
    StreamPart file,
    [Body] ...);
```

The interface becomes both:

```text
API contract
+
HTTP implementation metadata
```

---

## 2. REST-centric

Refit is optimized for HTTP REST APIs.

It is not a replacement for:

- gRPC
- WebSockets
- arbitrary TCP
- message brokers
- custom protocols

---

## 3. Abstraction leakage

Developers still need to understand:

```text
HttpClient
HttpMessageHandler
HttpRequestMessage
HttpContent
CancellationToken
DelegatingHandler
```

---

## 4. Generated-code debugging

Generated code can make unusual generator issues harder to debug.

---

## 5. Source-generator build requirements

Projects need compatible Roslyn/build tooling.

Legacy `packages.config` cannot load analyzers/source generators.

---

## 6. Complex APIs can become complicated

Simple REST APIs are where Refit shines.

Highly irregular APIs can produce:

```text
many attributes
custom formatters
custom serializers
handlers
reflection fallback
```

At some point, a manually implemented client may be clearer.

---

# 68. Architectural Boundaries

Refit should generally own:

```text
HTTP contract
request generation
serialization
response mapping
HTTP metadata
```

Application services should own:

```text
business rules
orchestration
domain decisions
use cases
```

Infrastructure should own:

```text
authentication
resilience
logging
telemetry
connectivity
caching
```

A clean architecture therefore looks like:

```text
UI
 |
 v
Application Service
 |
 v
Refit API Interface
 |
 v
HttpClient Pipeline
 |
 +-- Authentication
 +-- Logging
 +-- Resilience
 +-- Telemetry
 |
 v
Network
```

---

# 69. Recommended MAUI Architecture Using Refit

```text
Presentation
    |
    v
ViewModel
    |
    v
IUserService
    |
    v
IUserApi (Refit)
    |
    v
HttpClientFactory
    |
    +-- AuthHandler
    |
    +-- Retry/Resilience
    |
    +-- Logging
    |
    +-- Telemetry
    |
    v
HttpClient
    |
    v
REST API
```

The ViewModel should not know about:

```text
HttpRequestMessage
HttpResponseMessage
AuthorizationHeaderValue
```

unless there is a strong reason.

---

# 70. Where Refit Should Not Be Used

Avoid Refit when:

- API is highly dynamic
- request shape is generated dynamically at runtime
- protocol is not HTTP REST
- custom streaming transport is required
- generated interface becomes more complicated than handwritten code
- runtime reflection behavior is required and source generation is not suitable
- API requires unusual low-level HTTP control

Refit is an optimization for a specific class of problems, not a universal abstraction.

---

# 71. Design Lessons for Building a Refit-Like Library

For someone building a new .NET NuGet library, Refit provides several excellent design lessons.

## Lesson 1 — Start with a declarative contract

Good:

```csharp
[Get("/users/{id}")]
Task<User> GetUser(int id);
```

Bad:

```csharp
Task<User> GetUser(string arbitraryUrl);
```

The former provides analyzable structure.

---

## Lesson 2 — Generate code whenever possible

Avoid:

```text
Runtime reflection
```

when compile-time information is available.

---

## Lesson 3 — Keep a runtime escape hatch

Generated code cannot cover every scenario.

A fallback can preserve compatibility.

---

## Lesson 4 — Make the escape hatch optional

Refit's `Refit.Reflection` architecture is a good example.

Normal consumers do not have to pay for the reflection path.

---

## Lesson 5 — Integrate with standard .NET infrastructure

Instead of implementing:

```text
Custom DI
Custom HTTP transport
Custom retry engine
Custom logging
```

integrate with:

```text
IServiceCollection
IHttpClientFactory
HttpClient
DelegatingHandler
Microsoft resilience
ILogger
```

---

## Lesson 6 — Treat analyzers as part of the product

A library should detect invalid configurations as early as possible.

---

## Lesson 7 — Build testing support around the abstraction

A Refit-aware testing package is more useful than forcing every consumer to manually mock `HttpMessageHandler`.

---

# 72. Opportunity Analysis: Building a Better Refit for MAUI

A direct Refit clone would be difficult to justify.

A more interesting product would be:

> **A mobile-first source-generated API client framework built on top of HttpClient, inspired by Refit but designed around offline, resilience, lifecycle, diagnostics and mobile networking.**

Potential architecture:

```text
                 MAUI API Client Framework
                           |
          +----------------+----------------+
          |                |                |
     Source Gen       Connectivity       Resilience
          |                |                |
     API Client       Offline Mode       Retry
     Request Gen      Queue              Backoff
     Diagnostics      Cache              Timeout
          |                |                |
          +----------------+----------------+
                           |
                       HttpClient
                           |
                      API Server
```

---

# 73. Potential Differentiating Features

## 73.1 Offline Queue

```text
API call
   |
No network
   |
   v
Persistent Queue
   |
Network returns
   |
   v
Automatic replay
```

This would be a significant MAUI-specific differentiator.

---

## 73.2 Connectivity-Aware Requests

Allow:

```csharp
[RequiresNetwork]
[NetworkPolicy(NetworkPolicyType.Unmetered)]
```

or similar semantics.

---

## 73.3 Lifecycle-Aware Cancellation

Automatically connect requests to:

```text
Page lifecycle
ViewModel lifetime
CancellationToken
```

---

## 73.4 Automatic Token Refresh

Instead of every application implementing:

```text
401
 |
Refresh token
 |
Retry
```

the library could provide a safe, configurable pipeline.

Care would be needed to avoid infinite retry loops and concurrent refresh storms.

---

## 73.5 Request Deduplication

If multiple consumers request:

```text
GET /users/42
```

simultaneously:

```text
Request 1 ----+
Request 2 ----+----> One network request
Request 3 ----+
```

This can reduce mobile bandwidth and server load.

---

## 73.6 Cache Policy

Possible attributes:

```text
[Cache]
[Cache(Duration = ...)]
[NetworkFirst]
[CacheFirst]
[StaleWhileRevalidate]
```

---

## 73.7 Upload Progress

For MAUI:

```csharp
Task Upload(
    StreamPart file,
    IProgress<double> progress);
```

with platform-aware progress reporting.

---

## 73.8 Resumable Uploads

A mobile-first client could support:

```text
5 MB chunk
      |
Upload
      |
Network lost
      |
Resume from chunk N
```

This is much more valuable for mobile than a generic REST abstraction.

---

## 73.9 Diagnostics

Automatically collect:

```text
Endpoint
HTTP method
duration
status
payload size
response size
retry count
network type
cache hit/miss
failure category
```

without logging sensitive payloads.

---

## 73.10 Security-Aware Logging

Automatically redact:

```text
Authorization
Cookie
API Key
Access Token
Refresh Token
Sensitive query parameters
```

This should be designed into the logging pipeline rather than added later.

---

# 74. Suggested Architecture for a New Library

```text
MyApiClient/
|
+-- Core/
|   |
|   +-- Attributes
|   +-- Interfaces
|   +-- Request/Response abstractions
|   +-- Settings
|   +-- Exceptions
|
+-- SourceGenerator/
|   |
|   +-- Interface parser
|   +-- Semantic model
|   +-- Request emitter
|   +-- Response emitter
|   +-- Diagnostics
|
+-- Http/
|   |
|   +-- HttpClient integration
|   +-- Handlers
|   +-- Resilience
|
+-- Mobile/
|   |
|   +-- Connectivity
|   +-- Offline queue
|   +-- Lifecycle
|   +-- Cache
|
+-- Testing/
|   |
|   +-- Stub server
|   +-- Request verification
|   +-- Fault injection
|
+-- Serialization/
|   |
|   +-- System.Text.Json
|   +-- Custom serializers
|
+-- Benchmarks/
|
+-- Samples/
|
+-- Documentation/
```

---

# 75. Recommended Package Split

A good package design would be:

```text
MyApiClient
MyApiClient.HttpClientFactory
MyApiClient.Testing
MyApiClient.NewtonsoftJson
MyApiClient.SourceGenerator
MyApiClient.Maui
MyApiClient.Offline
MyApiClient.Resilience
```

The MAUI-specific functionality should not unnecessarily pollute the core package.

---

# 76. Refit vs Hypothetical MAUI-First Library

| Capability | Refit | MAUI-first opportunity |
|---|---|---|
| Type-safe REST | Excellent | Must retain |
| Source generation | Excellent | Must retain |
| HttpClientFactory | Excellent | Must retain |
| System.Text.Json | Excellent | Must retain |
| Multipart | Excellent | Must retain |
| Streaming | Excellent | Must retain |
| Testing | Excellent | Must retain |
| AOT/trimming | Excellent in modern versions | Must retain |
| Offline queue | Not core | Strong differentiator |
| Persistent cache | Not core | Strong differentiator |
| Connectivity awareness | Not core | Strong differentiator |
| Lifecycle cancellation | Not core | Strong differentiator |
| Resumable upload | Not core | Strong differentiator |
| Mobile diagnostics | Limited/core HTTP tooling | Strong differentiator |
| Network-aware policy | Not core | Strong differentiator |
| Background synchronization | Not core | Strong differentiator |
| Request deduplication | Not core | Strong differentiator |

---

# 77. Important Lessons from Refit's Evolution

The Refit project demonstrates a broader trend in modern .NET libraries:

```text
Reflection-heavy
      |
      v
Source-generated
      |
      v
Analyzer-assisted
      |
      v
AOT-aware
      |
      v
Allocation-conscious
```

This is likely to be the direction of many high-quality .NET libraries.

For a new library, source generation should therefore be considered from the beginning rather than retrofitted later.

---

# 78. Key Technical Risks When Building a Refit Alternative

## 1. Attribute explosion

Too many attributes can make the API difficult to understand.

## 2. Generator complexity

A mature generator must support many combinations:

```text
HTTP method
route
path
query
headers
body
multipart
serialization
return types
cancellation
generic interfaces
inheritance
streaming
```

The number of combinations grows rapidly.

## 3. Compiler compatibility

Roslyn analyzer packaging is surprisingly complex.

## 4. AOT

It is not enough to generate the client. Every supporting path must also be AOT-safe.

## 5. Serialization

Source-generated HTTP code does not automatically make JSON serialization AOT-safe.

## 6. Error semantics

Transport and server errors must remain distinguishable.

## 7. Testing

Generated clients need generator tests as well as runtime tests.

---

# 79. Testing Strategy for a Refit-Like Library

A robust project should have:

```text
Unit tests
Generator tests
Snapshot tests
Integration tests
Performance benchmarks
AOT tests
Trimming tests
Package tests
Analyzer tests
```

Example:

```text
Interface
   |
   v
Expected generated code
   |
   v
Snapshot comparison
```

And:

```text
Generated client
   |
   v
StubHttp
   |
   v
Verify request
```

---

# 80. Benchmark Strategy

Measure at least:

```text
Client creation
First request
Repeated request
Request construction
Query generation
Path formatting
Header generation
Serialization
Deserialization
Multipart creation
Generated client
Reflection client
```

Also measure:

```text
Allocated bytes
Gen0 collections
Execution time
Throughput
```

Generator benchmarks should separately measure:

```text
Parse
Semantic model
Code generation
Incremental rebuild
Cold build
```

---

# 81. Security Checklist

A Refit-like library should explicitly test:

```text
Authorization header leakage
Exception redaction
Logging redaction
URL credential leakage
SSRF considerations for dynamic URLs
XML XXE
Unsafe polymorphic deserialization
Header injection
Path encoding
Query encoding
Multipart filename handling
Request-body compression behavior
```

Dynamic `[Url]` support deserves special attention because arbitrary URLs can change the trust boundary of a client.

---

# 82. Performance Checklist

Generated code should avoid unnecessary:

```text
Reflection
Boxing
Allocations
Delegates
Intermediate strings
Descriptor objects
Repeated metadata lookup
```

But optimization should be evidence-driven.

Benchmark before and after.

---

# 83. API Design Recommendation

For a new library, prefer:

```csharp
[Get("/users/{id}")]
Task<User> GetUser(int id);
```

over:

```csharp
Task<User> GetUser(
    string method,
    string route,
    object parameters);
```

The first form provides information at compile time.

That information can drive:

```text
Analyzer
Source generator
Documentation generator
Testing
IDE tooling
```

---

# 84. Potential Future Direction

A modern API client generator could eventually generate more than HTTP code.

From one interface:

```csharp
public interface IUserApi
{
    [Get("/users/{id}")]
    Task<User> GetUser(int id);
}
```

the generator could potentially produce:

```text
HTTP client
Mock client
Testing metadata
Telemetry metadata
Documentation
OpenAPI fragments
Endpoint metrics
Cache metadata
Resilience metadata
```

This is where source generation becomes much more powerful than simply eliminating reflection.

---

# 85. Overall Assessment

### Architecture

**9/10**

Strong separation between API declaration and HTTP infrastructure.

### Developer experience

**9.5/10**

Very concise and readable for conventional REST APIs.

### Performance

**9/10**

Modern generated request building is a major improvement.

### AOT/trimming

**9/10**

Strong modern architecture, especially when using generated-only APIs.

### Extensibility

**9/10**

Excellent integration with standard .NET HTTP infrastructure.

### Mobile/MAUI specialization

**7/10**

Works very well with MAUI, but is not designed specifically around mobile constraints.

### Testing

**9/10**

First-party `Refit.Testing` significantly improves the story.

### Complexity

**7/10**

The simple API is easy; the underlying implementation and edge cases are complex.

---

# 86. Final Conclusion

Refit is much more than an attribute-based REST client.

Its most important architectural evolution is:

```text
Runtime reflection
        ↓
Source-generated client
        ↓
Source-generated request construction
        ↓
Analyzer validation
        ↓
AOT/trimming-friendly architecture
```

That makes modern Refit an excellent reference project for anyone designing a source-generated .NET library.

For a new library, the biggest lesson is **not to copy Refit feature-for-feature**.

Instead:

1. Keep the declarative API model.
2. Use Roslyn source generation from day one.
3. Generate request construction, not just client wrappers.
4. Provide analyzers.
5. Keep runtime reflection optional.
6. Integrate deeply with `HttpClientFactory`.
7. Provide first-party testing.
8. Design for trimming/AOT.
9. Keep serialization modular.
10. Add a strong domain-specific layer where Refit is intentionally generic.

For .NET MAUI specifically, the strongest opportunity is a **mobile-first API framework** that preserves Refit's excellent type-safe/source-generated REST experience while adding:

```text
Connectivity
Offline queue
Persistent cache
Lifecycle cancellation
Mobile resilience
Request deduplication
Resumable uploads
Background synchronization
Network-aware policies
Mobile diagnostics
```

That would be a substantially different product rather than another Refit clone.

---

# 87. Primary Sources

1. Refit GitHub repository  
   https://github.com/reactiveui/refit

2. Refit documentation  
   https://reactiveui.github.io/refit/

3. Refit README  
   https://github.com/reactiveui/refit/blob/main/README.md

4. Refit breaking changes / release notes  
   https://github.com/reactiveui/refit/blob/main/docs/breaking-changes.md

5. Refit testing documentation  
   https://github.com/reactiveui/refit/blob/main/docs/testing.md

6. Refit NuGet package  
   https://www.nuget.org/packages/Refit/

7. Refit.HttpClientFactory NuGet package  
   https://www.nuget.org/packages/Refit.HttpClientFactory/

8. Refit.Newtonsoft.Json NuGet package  
   https://www.nuget.org/packages/Refit.Newtonsoft.Json/

9. Refit.Xml NuGet package  
   https://www.nuget.org/packages/Refit.Xml/

10. Refit source-generator contributor documentation  
    https://github.com/reactiveui/refit/blob/main/CLAUDE.md

---

## Research Notes

This document focuses on the public repository, official documentation, package metadata, release notes and source-generator architecture available as of 4 September 2026.

The analysis distinguishes documented behavior from architectural inference. Performance conclusions should be validated with the repository's own benchmarks or application-specific benchmarks before being used as hard numerical claims.

