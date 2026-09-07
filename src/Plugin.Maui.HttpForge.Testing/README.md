# Plugin.Maui.HttpForge.Testing

Stub `HttpClient` helpers for [Plugin.Maui.HttpForge](https://www.nuget.org/packages/Plugin.Maui.HttpForge) generated clients.

```csharp
var http = new StubHttp
{
    { Route.Get("/users/{id}"), Reply.With(new User { Id = 7, Name = "octocat" }) }
};

var api = http.CreateClient<IUserApi>("https://api.example.com");
var user = await api.GetUser(7);
await http.VerifyAllCalledAsync();
```
