using System.Net;
using Microsoft.Extensions.DependencyInjection;

namespace Plugin.Maui.HttpForge.Tests;

public sealed class RuntimeHelperTests
{
    [Fact]
    public void QueryBuilder_OmitsNullsAndEncodes()
    {
        var query = new HttpForgeQueryBuilder()
            .Add("q", "tea & milk")
            .Add("page", 2)
            .Add("empty", null);

        Assert.Equal("?q=tea%20%26%20milk&page=2", query.ToString());
    }

    [Fact]
    public async Task Serializer_RoundTripsObject()
    {
        var serializer = new SystemTextJsonContentSerializer();
        using var content = await serializer.ToHttpContentAsync(new { Name = "Tea" });
        var json = await content.ReadAsStringAsync();
        Assert.Contains("\"name\":\"Tea\"", json);
    }

    [Fact]
    public void ApiException_RedactsUserInfo()
    {
        var redacted = ApiException.RedactUri(new Uri("https://user:secret@example.com/x"));
        Assert.DoesNotContain("secret", redacted);
        Assert.Contains("***", redacted);
    }

    [Fact]
    public async Task Invoker_DeserializesSuccess()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("ok", System.Text.Encoding.UTF8, "text/plain")
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        using var request = new HttpRequestMessage(HttpMethod.Get, "ping");
        var result = await HttpForgeInvoker.SendAsync<string>(client, request, new HttpForgeSettings(), CancellationToken.None);
        Assert.Equal("ok", result);
    }

    [Fact]
    public void RestService_UnregisteredType_Throws()
    {
        var client = new HttpClient { BaseAddress = new Uri("https://api.test/") };
        Assert.Throws<InvalidOperationException>(() => RestService.For<IDisposable>(client));
    }

    [Fact]
    public void AddHttpForge_RegistersSettings()
    {
        var services = new ServiceCollection();
        services.AddHttpForge(settings => settings.ExceptionFactory = (_, _) => Task.FromResult<Exception?>(null));
        var provider = services.BuildServiceProvider();
        var settings = provider.GetRequiredService<HttpForgeSettings>();
        Assert.NotNull(settings.ExceptionFactory);
    }
}
