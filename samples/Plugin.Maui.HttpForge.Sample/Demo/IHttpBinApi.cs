using System.Text.Json;
using Plugin.Maui.HttpForge;

namespace Plugin.Maui.HttpForge.Sample.Demo;

public sealed class HttpBinResponse
{
    public string? Url { get; set; }
    public Dictionary<string, JsonElement>? Args { get; set; }
    public Dictionary<string, JsonElement>? Headers { get; set; }
    public Dictionary<string, JsonElement>? Form { get; set; }
    public Dictionary<string, JsonElement>? Files { get; set; }
    public string? Data { get; set; }
}

public sealed class EchoQuery
{
    public string? DisplayName { get; set; }
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

public sealed class HttpBinStreamItem
{
    public int Id { get; set; }
    public string? Url { get; set; }
}

/// <summary>
/// Live echo calls to https://httpbin.org for the 1.1 contract surface.
/// </summary>
[Headers("Accept: application/json")]
public interface IHttpBinApi
{
    [Get("/get")]
    Task<HttpBinResponse> SearchAsync([Query] EchoQuery query, CancellationToken cancellationToken = default);

    [Get("/get")]
    Task<HttpBinResponse> SearchAgesAsync([Query(CollectionFormat.Multi)] int[] ages, CancellationToken cancellationToken = default);

    [Get("/get")]
    Task<HttpBinResponse> SearchAgesCsvAsync([Query("ages", CollectionFormat.Csv)] int[] ages, CancellationToken cancellationToken = default);

    [Get("/anything/users/{id}/orders/{orderId?}")]
    Task<HttpBinResponse> GetOrdersAsync(int id, int? orderId, CancellationToken cancellationToken = default);

    [Get("/")]
    Task<HttpBinResponse> GetFromAbsoluteAsync([Url] string url, CancellationToken cancellationToken = default);

    [Get("/get")]
    Task<HttpBinResponse> ListFlagsAsync([QueryName] string flag, [QueryName] bool archived = false, CancellationToken cancellationToken = default);

    [Get("/get")]
    Task<HttpBinResponse> GetWithAuthAsync(CancellationToken cancellationToken = default);

    [Timeout(2_000)]
    [Get("/delay/8")]
    Task<HttpBinResponse> SlowAsync(CancellationToken cancellationToken = default);

    [Multipart]
    [Post("/post")]
    Task<HttpBinResponse> UploadAsync([AliasAs("file")] StreamPart file, CancellationToken cancellationToken = default);

    [Multipart]
    [Post("/post")]
    Task<HttpBinResponse> SaveProfileAsync([FormObject] ProfileForm form, CancellationToken cancellationToken = default);

    [CompressRequest(RequestBodyCompression.Gzip)]
    [Post("/post")]
    Task<HttpBinResponse> CreateCompressedAsync([Body] CreatePostRequest request, CancellationToken cancellationToken = default);

    [Post("/post")]
    Task<HttpBinResponse> SendBatchAsync([Body(BodySerializationMethod.JsonLines)] IEnumerable<EventItem> items, CancellationToken cancellationToken = default);

    [Get("/stream/3")]
    IAsyncEnumerable<HttpBinStreamItem> StreamEventsAsync(CancellationToken cancellationToken = default);
}
