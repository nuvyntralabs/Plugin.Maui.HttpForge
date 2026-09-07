using System.Net;
using Plugin.Maui.HttpForge.NewtonsoftJson;
using Plugin.Maui.HttpForge.Testing;
using Plugin.Maui.HttpForge.Tests.Apis;
using Plugin.Maui.HttpForge.Xml;

namespace Plugin.Maui.HttpForge.Tests;

public sealed class OptionalPackagesTests
{
    [Fact]
    public async Task StubHttp_CreatesClientAndVerifiesRoutes()
    {
        var http = new StubHttp
        {
            { Route.Get("/products/{id}"), Reply.With(new Product { Id = 7, Name = "Tea" }) }
        };

        var api = http.CreateClient<ICatalogApi>("https://api.example.com");
        var product = await api.GetProduct(7);

        Assert.Equal(7, product.Id);
        Assert.Equal("Tea", product.Name);
        await http.VerifyAllCalledAsync();
    }

    [Fact]
    public async Task NewtonsoftSerializer_RoundTrips()
    {
        var serializer = new NewtonsoftJsonContentSerializer();
        using var content = await serializer.ToHttpContentAsync(new Product { Id = 2, Name = "Coffee" });
        var product = await serializer.FromHttpContentAsync<Product>(content);
        Assert.Equal("Coffee", product!.Name);
    }

    [Fact]
    public async Task XmlSerializer_RoundTrips()
    {
        var serializer = new XmlContentSerializer();
        using var content = await serializer.ToHttpContentAsync(new Product { Id = 3, Name = "Milk" });
        var xml = await content.ReadAsStringAsync();
        Assert.Contains("<Name>Milk</Name>", xml);

        using var encoded = new StringContent(xml);
        var product = await serializer.FromHttpContentAsync<Product>(encoded);
        Assert.Equal(3, product!.Id);
    }

    [Fact]
    public async Task StubHttp_UnmatchedRoute_Throws()
    {
        var http = new StubHttp();
        var api = http.CreateClient<ICatalogApi>("https://api.example.com");
        var ex = await Assert.ThrowsAsync<ApiRequestException>(() => api.GetProduct(1));
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }
}
