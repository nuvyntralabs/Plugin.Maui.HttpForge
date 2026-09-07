using System.Net;
using System.Net.Http;
using System.Text;

namespace Plugin.Maui.HttpForge.Testing;

/// <summary>
/// Response recipe for a stubbed route.
/// </summary>
public sealed class Reply
{
    private readonly Func<HttpRequestMessage, IHttpContentSerializer, CancellationToken, Task<HttpResponseMessage>> _create;
    private TimeSpan _delay;

    private Reply(Func<HttpRequestMessage, IHttpContentSerializer, CancellationToken, Task<HttpResponseMessage>> create)
    {
        _create = create;
    }

    public static Reply With<T>(T body, HttpStatusCode statusCode = HttpStatusCode.OK)
        => new(async (_, serializer, cancellationToken) =>
        {
            var content = await serializer.ToHttpContentAsync(body, cancellationToken).ConfigureAwait(false);
            return new HttpResponseMessage(statusCode) { Content = content };
        });

    public static Reply With(string body, string mediaType = "text/plain", HttpStatusCode statusCode = HttpStatusCode.OK)
        => new((_, _, _) => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, mediaType)
        }));

    public static Reply WithStatus(HttpStatusCode statusCode)
        => new((_, _, _) => Task.FromResult(new HttpResponseMessage(statusCode)));

    public static Reply WithFault(Exception exception)
        => new((_, _, _) => throw exception);

    public Reply Delay(TimeSpan latency)
    {
        _delay = latency;
        return this;
    }

    internal async Task<HttpResponseMessage> ExecuteAsync(
        HttpRequestMessage request,
        IHttpContentSerializer serializer,
        CancellationToken cancellationToken)
    {
        if (_delay > TimeSpan.Zero)
            await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);

        return await _create(request, serializer, cancellationToken).ConfigureAwait(false);
    }
}
