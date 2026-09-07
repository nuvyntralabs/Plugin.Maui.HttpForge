using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Plugin.Maui.HttpForge;

namespace Plugin.Maui.HttpForge.NewtonsoftJson;

/// <summary>
/// <see cref="IHttpContentSerializer"/> backed by Newtonsoft.Json.
/// </summary>
public sealed class NewtonsoftJsonContentSerializer : IHttpContentSerializer
{
    private readonly JsonSerializerSettings _settings;

    public NewtonsoftJsonContentSerializer(JsonSerializerSettings? settings = null)
    {
        _settings = settings ?? new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
        };
    }

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
            return Task.FromResult<HttpContent>(new StringContent(text, Encoding.UTF8, "text/plain"));

        var json = JsonConvert.SerializeObject(value, _settings);
        return Task.FromResult<HttpContent>(new StringContent(json, Encoding.UTF8, "application/json"));
    }

    public async Task<T?> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken = default)
    {
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

        var payload = await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(payload))
            return default;

        return JsonConvert.DeserializeObject<T>(payload, _settings);
    }
}
