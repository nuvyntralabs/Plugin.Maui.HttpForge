using System.IO.Compression;
using System.Net;
using System.Text;
using Plugin.Maui.HttpForge.Tests.Apis;

namespace Plugin.Maui.HttpForge.Tests;

public sealed class AdvancedClientTests
{
    [Fact]
    public async Task Search_FlattensQueryObject_AndAppliesPrefix()
    {
        HttpRequestMessage? captured = null;
        var api = Create(request =>
        {
            captured = request;
            return Json("[]");
        });

        await api.Search(new UserQuery { Name = "tea", Page = 2 });

        Assert.Equal("https://api.test/api/v1/users?Name=tea&Page=2", captured!.RequestUri!.ToString());
    }

    [Fact]
    public async Task Search_AppliesSnakeCaseKeyFormatter()
    {
        HttpRequestMessage? captured = null;
        var api = Create(request =>
        {
            captured = request;
            return Json("[]");
        }, settings => settings.UrlParameterKeyFormatter = UrlParameterKeyFormatter.SnakeCase);

        await api.Search(new UserQuery { Name = "tea", Page = 2 });

        Assert.Equal("https://api.test/api/v1/users?name=tea&page=2", captured!.RequestUri!.ToString());
    }

    [Fact]
    public async Task SearchAges_WritesMultiAndCsv()
    {
        var uris = new List<string>();
        var api = Create(request =>
        {
            uris.Add(request.RequestUri!.ToString());
            return Json("[]");
        });

        await api.SearchAges([1, 2]);
        await api.SearchAgesCsv([1, 2]);

        Assert.Equal("https://api.test/api/v1/users?ages=1&ages=2", uris[0]);
        Assert.Equal("https://api.test/api/v1/users?ages=1%2C2", uris[1]);
    }

    [Fact]
    public async Task GetOrders_OmitsOptionalSegmentWhenNull()
    {
        HttpRequestMessage? withValue = null;
        HttpRequestMessage? without = null;
        var api = Create(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/9", StringComparison.Ordinal))
                withValue = request;
            else
                without = request;
            return Json("[]");
        });

        await api.GetOrders(4, 9);
        await api.GetOrders(4, null);

        Assert.Equal("https://api.test/api/v1/users/4/orders/9", withValue!.RequestUri!.ToString());
        Assert.Equal("https://api.test/api/v1/users/4/orders", without!.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetFromAbsolute_UsesUrlParameter()
    {
        HttpRequestMessage? captured = null;
        var api = Create(request =>
        {
            captured = request;
            return Json("""{"id":1,"name":"Tea"}""");
        });

        await api.GetFromAbsolute("https://other.test/users/3");

        Assert.Equal("https://other.test/users/3", captured!.RequestUri!.ToString());
    }

    [Fact]
    public async Task ListFlags_WritesValuelessQuery()
    {
        HttpRequestMessage? captured = null;
        var api = Create(request =>
        {
            captured = request;
            return Json("[]");
        });

        await api.ListFlags("archived", archived: true);

        Assert.Equal("https://api.test/api/v1/items?archived&archived", captured!.RequestUri!.ToString());
    }

    [Fact]
    public async Task SaveProfile_FlattensFormObject()
    {
        HttpRequestMessage? captured = null;
        string? text = null;
        var api = Create(request =>
        {
            captured = request;
            text = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        await api.SaveProfile(new ProfileForm
        {
            Name = "Ada",
            Address = new AddressForm { City = "London" }
        });

        Assert.IsType<MultipartFormDataContent>(captured!.Content);
        Assert.Contains("Ada", text);
        Assert.Contains("London", text);
        Assert.Contains("address.city", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StreamEvents_ReadsJsonLines()
    {
        var api = Create(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                {"id":1,"name":"one"}
                {"id":2,"name":"two"}
                """, Encoding.UTF8, "application/x-ndjson")
        });

        var items = new List<EventItem>();
        await foreach (var item in api.StreamEvents())
            items.Add(item);

        Assert.Equal(2, items.Count);
        Assert.Equal("two", items[1].Name);
    }

    [Fact]
    public async Task StreamEvents_ReadsSse()
    {
        var api = Create(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    data: {"id":8,"name":"sse"}

                    """, Encoding.UTF8, "text/event-stream")
            };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/event-stream");
            return response;
        });

        var items = new List<EventItem>();
        await foreach (var item in api.StreamEvents())
            items.Add(item);

        Assert.Equal(8, Assert.Single(items).Id);
    }

    [Fact]
    public async Task SendBatch_WritesJsonLines()
    {
        string? body = null;
        var api = Create(request =>
        {
            body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        await api.SendBatch(
        [
            new EventItem { Id = 1, Name = "a" },
            new EventItem { Id = 2, Name = "b" }
        ]);

        Assert.Contains("\"id\":1", body);
        Assert.Contains('\n', body!);
        Assert.Contains("\"id\":2", body);
    }

    [Fact]
    public async Task CreateCompressed_SetsGzipContentEncoding()
    {
        string? encoding = null;
        string? json = null;
        var api = Create(request =>
        {
            encoding = string.Join(",", request.Content!.Headers.ContentEncoding);
            using var gzip = new GZipStream(request.Content.ReadAsStream(), CompressionMode.Decompress);
            using var reader = new StreamReader(gzip);
            json = reader.ReadToEnd();
            return Json("""{"id":1,"name":"Tea"}""");
        });

        await api.CreateCompressed(new CreateProductRequest { Name = "Tea" });

        Assert.Contains("gzip", encoding);
        Assert.Contains("Tea", json);
    }

    [Fact]
    public async Task AuthorizationHeaderValueGetter_SetsHeader()
    {
        HttpRequestMessage? captured = null;
        var api = Create(request =>
        {
            captured = request;
            return Json("""{"id":1,"name":"Tea"}""");
        }, settings => settings.AuthorizationHeaderValueGetter = (_, _) => Task.FromResult<string?>("Bearer token-1"));

        await api.GetUser(1);

        Assert.Equal("Bearer token-1", captured!.Headers.GetValues("Authorization").Single());
    }

    private static IAdvancedApi Create(Func<HttpRequestMessage, HttpResponseMessage> send, Action<HttpForgeSettings>? configure = null)
    {
        var client = new HttpClient(new StubHandler(send))
        {
            BaseAddress = new Uri("https://api.test/")
        };
        var settings = new HttpForgeSettings();
        configure?.Invoke(settings);
        Assert.True(HttpForgeClientRegistry.IsRegistered<IAdvancedApi>());
        return RestService.For<IAdvancedApi>(client, settings);
    }

    private static HttpResponseMessage Json(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
}
