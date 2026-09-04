using System.Net;
using System.Text;
using System.Text.Json;

namespace Plugin.Maui.HttpForge.Sample.Demo;

internal sealed class DemoApiHandler : HttpMessageHandler
{
    private readonly List<Product> _products =
    [
        new() { Id = 1, Name = "Assam tea" },
        new() { Id = 2, Name = "Filter coffee" }
    ];

    private int _nextId = 3;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? "/";
        if (request.Method == HttpMethod.Get && path == "/products")
            return Json(HttpStatusCode.OK, _products);

        if (request.Method == HttpMethod.Get && path.StartsWith("/products/", StringComparison.Ordinal))
        {
            var id = int.Parse(path.Split('/')[2]);
            var product = _products.FirstOrDefault(p => p.Id == id);
            return product is null
                ? Json(HttpStatusCode.NotFound, new { message = "Not found" })
                : Json(HttpStatusCode.OK, product);
        }

        if (request.Method == HttpMethod.Post && path == "/products")
        {
            var json = request.Content is null ? "{}" : await request.Content.ReadAsStringAsync(cancellationToken);
            var body = JsonSerializer.Deserialize<CreateProductRequest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                       ?? new CreateProductRequest();
            var product = new Product { Id = _nextId++, Name = string.IsNullOrWhiteSpace(body.Name) ? "Untitled" : body.Name };
            _products.Add(product);
            return Json(HttpStatusCode.Created, product);
        }

        if (request.Method == HttpMethod.Post && path.Contains("/photo", StringComparison.Ordinal))
            return new HttpResponseMessage(HttpStatusCode.NoContent);

        return Json(HttpStatusCode.NotFound, new { message = path });
    }

    private static HttpResponseMessage Json(HttpStatusCode status, object value)
        => new(status)
        {
            Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json")
        };
}
