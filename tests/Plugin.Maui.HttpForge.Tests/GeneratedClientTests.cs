using System.Net;
using System.Text;
using Plugin.Maui.HttpForge.Tests.Apis;

namespace Plugin.Maui.HttpForge.Tests;

public sealed class GeneratedClientTests
{
    [Fact]
    public async Task Get_SubstitutesPathAndDeserializesJson()
    {
        HttpRequestMessage? captured = null;
        var api = Create(request =>
        {
            captured = request;
            return Json(HttpStatusCode.OK, """{"id":7,"name":"Tea"}""");
        });

        var product = await api.GetProduct(7);

        Assert.Equal(HttpMethod.Get, captured!.Method);
        Assert.Equal("https://api.test/products/7", captured.RequestUri!.ToString());
        Assert.Equal(7, product.Id);
        Assert.Equal("Tea", product.Name);
        Assert.Contains(captured.Headers.Accept, v => v.MediaType == "application/json");
    }

    [Fact]
    public async Task Search_AppendsQueryStringAndOmitsNulls()
    {
        HttpRequestMessage? captured = null;
        var api = Create(request =>
        {
            captured = request;
            return Json(HttpStatusCode.OK, """[{"id":1,"name":"Tea"}]""");
        });

        var results = await api.Search(null, 2);

        Assert.Equal("https://api.test/products?page=2", captured!.RequestUri!.ToString());
        Assert.Single(results);
    }

    [Fact]
    public async Task Post_SerializesBody()
    {
        HttpRequestMessage? captured = null;
        string? body = null;
        var api = Create(request =>
        {
            captured = request;
            body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return Json(HttpStatusCode.Created, """{"id":3,"name":"Coffee"}""");
        });

        var created = await api.Create(new CreateProductRequest { Name = "Coffee" });

        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Contains("\"name\":\"Coffee\"", body);
        Assert.Equal("Coffee", created.Name);
    }

    [Fact]
    public async Task Delete_SendsHeaderAndThrowsApiExceptionOnError()
    {
        var api = Create(request =>
        {
            Assert.Equal("abc", request.Headers.GetValues("X-Request-Id").Single());
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("missing", Encoding.UTF8, "text/plain"),
                RequestMessage = request
            };
        });

        var ex = await Assert.ThrowsAsync<ApiException>(() => api.Delete(9, "abc"));
        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
        Assert.Equal("missing", ex.Content);
    }

    [Fact]
    public async Task GetProductResponse_DoesNotThrowOn404()
    {
        var api = Create(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("gone")
        });

        using var response = await api.GetProductResponse(4);
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("gone", response.ErrorContent);
    }

    [Fact]
    public async Task UploadPhoto_SendsMultipart()
    {
        HttpRequestMessage? captured = null;
        string? text = null;
        var api = Create(request =>
        {
            captured = request;
            text = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        using var stream = new MemoryStream("hello"u8.ToArray());
        await api.UploadPhoto(5, new StreamPart(stream, "photo.jpg", "image/jpeg"));

        Assert.IsType<MultipartFormDataContent>(captured!.Content);
        Assert.Contains("photo.jpg", text);
        Assert.Equal("https://api.test/products/5/photo", captured.RequestUri!.ToString());
    }

    [Fact]
    public async Task TransportFailure_BecomesApiRequestException()
    {
        var api = Create((_, _) => throw new HttpRequestException("offline"));

        var ex = await Assert.ThrowsAsync<ApiRequestException>(() => api.GetProduct(1));
        Assert.IsType<HttpRequestException>(ex.InnerException);
    }

    private static ICatalogApi Create(Func<HttpRequestMessage, HttpResponseMessage> send)
        => Create((request, _) => Task.FromResult(send(request)));

    private static ICatalogApi Create(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
    {
        var client = new HttpClient(new StubHandler(send))
        {
            BaseAddress = new Uri("https://api.test/")
        };

        Assert.True(HttpForgeClientRegistry.IsRegistered<ICatalogApi>());
        return RestService.For<ICatalogApi>(client);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string json)
        => new(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
}
