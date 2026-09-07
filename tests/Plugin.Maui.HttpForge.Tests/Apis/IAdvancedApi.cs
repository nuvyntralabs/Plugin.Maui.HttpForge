using Plugin.Maui.HttpForge;

namespace Plugin.Maui.HttpForge.Tests.Apis;

public sealed class UserQuery
{
    public string? Name { get; set; }
    public int Page { get; set; }
}

public sealed class ProfileForm
{
    public string Name { get; set; } = "";
    public AddressForm Address { get; set; } = new();
}

public sealed class AddressForm
{
    public string City { get; set; } = "";
}

public sealed class EventItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

[PathPrefix("/api/v1")]
[Headers("Accept: application/json")]
public interface IAdvancedApi
{
    [Get("/users")]
    Task<List<Product>> Search([Query] UserQuery query, CancellationToken cancellationToken = default);

    [Get("/users")]
    Task<List<Product>> SearchAges([Query(CollectionFormat.Multi)] int[] ages, CancellationToken cancellationToken = default);

    [Get("/users")]
    Task<List<Product>> SearchAgesCsv([Query(CollectionFormat.Csv)] int[] ages, CancellationToken cancellationToken = default);

    [Get("/users/{id}/orders/{orderId?}")]
    Task<List<Product>> GetOrders(int id, int? orderId, CancellationToken cancellationToken = default);

    [Timeout(5_000)]
    [Get("/users/{id}")]
    Task<Product> GetUser(int id, CancellationToken cancellationToken = default);

    [Get("/")]
    Task<Product> GetFromAbsolute([Url] string url, CancellationToken cancellationToken = default);

    [Get("/items")]
    Task<List<Product>> ListFlags([QueryName] string flag, [QueryName] bool archived = false, CancellationToken cancellationToken = default);

    [Multipart]
    [Post("/profile")]
    Task SaveProfile([FormObject] ProfileForm form, CancellationToken cancellationToken = default);

    [Get("/events")]
    IAsyncEnumerable<EventItem> StreamEvents(CancellationToken cancellationToken = default);

    [Post("/batch")]
    Task SendBatch([Body(BodySerializationMethod.JsonLines)] IEnumerable<EventItem> items, CancellationToken cancellationToken = default);

    [CompressRequest(RequestBodyCompression.Gzip)]
    [Post("/users")]
    Task<Product> CreateCompressed([Body] CreateProductRequest request, CancellationToken cancellationToken = default);
}
