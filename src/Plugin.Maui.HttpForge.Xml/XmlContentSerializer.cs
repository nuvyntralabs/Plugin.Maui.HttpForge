using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Plugin.Maui.HttpForge;

namespace Plugin.Maui.HttpForge.Xml;

/// <summary>
/// <see cref="IHttpContentSerializer"/> backed by <see cref="XmlSerializer"/>.
/// DTD processing is prohibited.
/// </summary>
public sealed class XmlContentSerializer : IHttpContentSerializer
{
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

        var serializer = new XmlSerializer(typeof(T));
        using var writer = new Utf8StringWriter();
        serializer.Serialize(writer, value);
        return Task.FromResult<HttpContent>(new StringContent(writer.ToString(), Encoding.UTF8, "application/xml"));
    }

    public async Task<T?> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken = default)
    {
        if (typeof(T) == typeof(string))
        {
            var text = await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return (T)(object)text;
        }

        await using var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreWhitespace = true
        });

        var serializer = new XmlSerializer(typeof(T));
        return (T?)serializer.Deserialize(reader);
    }

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
