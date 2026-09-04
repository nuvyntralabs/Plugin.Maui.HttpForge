using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Plugin.Maui.HttpForge;

/// <summary>
/// Default <see cref="IHttpContentSerializer"/> using <see cref="JsonSerializer"/>.
/// Supply a <see cref="JsonSerializerContext"/> for Native AOT / trimmed hosts.
/// </summary>
public sealed class SystemTextJsonContentSerializer : IHttpContentSerializer
{
    private readonly JsonSerializerOptions _options;
    private readonly JsonSerializerContext? _context;

    public SystemTextJsonContentSerializer(JsonSerializerOptions? options = null, JsonSerializerContext? context = null)
    {
        _options = options ?? CreateDefaultOptions();
        _context = context;
    }

    public static JsonSerializerOptions CreateDefaultOptions() => new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Hosts that trim or use Native AOT should pass JsonSerializerContext.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Hosts that trim or use Native AOT should pass JsonSerializerContext.")]
    public Task<HttpContent> ToHttpContentAsync<T>(T value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (value is HttpContent httpContent)
            return Task.FromResult(httpContent);

        if (value is Stream stream)
        {
            HttpContent content = new StreamContent(stream);
            content.Headers.ContentType ??= new MediaTypeHeaderValue("application/octet-stream");
            return Task.FromResult(content);
        }

        if (value is byte[] bytes)
        {
            HttpContent content = new ByteArrayContent(bytes);
            content.Headers.ContentType ??= new MediaTypeHeaderValue("application/octet-stream");
            return Task.FromResult(content);
        }

        if (value is string text)
        {
            return Task.FromResult<HttpContent>(new StringContent(text, Encoding.UTF8, "text/plain"));
        }

        var json = _context is not null
            ? JsonSerializer.Serialize(value, typeof(T), _context)
            : JsonSerializer.Serialize(value, _options);

        return Task.FromResult<HttpContent>(new StringContent(json, Encoding.UTF8, "application/json"));
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Hosts that trim or use Native AOT should pass JsonSerializerContext.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Hosts that trim or use Native AOT should pass JsonSerializerContext.")]
    public async Task<T?> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken = default)
    {
        if (typeof(T) == typeof(HttpResponseMessage))
            throw new InvalidOperationException("HttpResponseMessage must be returned by the invoker, not the serializer.");

        if (typeof(T) == typeof(string))
        {
            var text = await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return (T)(object)text;
        }

        if (typeof(T) == typeof(byte[]))
        {
            var bytes = await content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            return (T)(object)bytes;
        }

        if (typeof(T) == typeof(Stream))
        {
            var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return (T)(object)stream;
        }

        var payload = await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(payload))
            return default;

        if (_context is not null)
            return (T?)JsonSerializer.Deserialize(payload, typeof(T), _context);

        return JsonSerializer.Deserialize<T>(payload, _options);
    }
}
