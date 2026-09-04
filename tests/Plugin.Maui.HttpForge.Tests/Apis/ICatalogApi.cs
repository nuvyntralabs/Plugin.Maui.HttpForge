using Plugin.Maui.HttpForge;

namespace Plugin.Maui.HttpForge.Tests.Apis;

public sealed class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public sealed class CreateProductRequest
{
    public string Name { get; set; } = "";
}

[Headers("Accept: application/json")]
public interface ICatalogApi
{
    [Get("/products/{id}")]
    Task<Product> GetProduct(int id, CancellationToken cancellationToken = default);

    [Get("/products")]
    Task<List<Product>> Search(string? name, int page = 1, CancellationToken cancellationToken = default);

    [Post("/products")]
    Task<Product> Create([Body] CreateProductRequest request, CancellationToken cancellationToken = default);

    [Delete("/products/{id}")]
    Task Delete(int id, [Header("X-Request-Id")] string requestId, CancellationToken cancellationToken = default);

    [Get("/products/{id}")]
    Task<IApiResponse<Product>> GetProductResponse(int id, CancellationToken cancellationToken = default);

    [Multipart]
    [Post("/products/{id}/photo")]
    Task UploadPhoto(int id, [AliasAs("file")] StreamPart file, CancellationToken cancellationToken = default);
}
