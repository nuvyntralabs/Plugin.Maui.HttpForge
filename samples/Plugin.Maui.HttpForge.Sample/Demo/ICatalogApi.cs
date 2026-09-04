using Plugin.Maui.HttpForge;

namespace Plugin.Maui.HttpForge.Sample.Demo;

public sealed class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public sealed class CreateProductRequest
{
    public string Name { get; set; } = "";
}

public interface ICatalogApi
{
    [Get("/products")]
    Task<List<Product>> ListAsync(CancellationToken cancellationToken = default);

    [Get("/products/{id}")]
    Task<Product> GetAsync(int id, CancellationToken cancellationToken = default);

    [Post("/products")]
    Task<Product> CreateAsync([Body] CreateProductRequest request, CancellationToken cancellationToken = default);

    [Multipart]
    [Post("/products/{id}/photo")]
    Task UploadPhotoAsync(int id, [AliasAs("file")] StreamPart file, CancellationToken cancellationToken = default);
}
