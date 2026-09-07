using System.Collections;
using System.Net.Http;

namespace Plugin.Maui.HttpForge.Testing;

/// <summary>
/// In-memory <see cref="HttpMessageHandler"/> that creates generated HttpForge clients.
/// </summary>
public sealed class StubHttp : IEnumerable
{
    private readonly List<StubEntry> _entries = [];
    private readonly List<HttpRequestMessage> _requests = [];

    public IReadOnlyList<HttpRequestMessage> Requests => _requests;

    public void Add(Route route, Reply reply)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(reply);
        _entries.Add(new StubEntry(route, reply));
    }

    public T CreateClient<T>(string hostUrl, HttpForgeSettings? settings = null)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostUrl);
        var client = new HttpClient(new StubHandler(this, settings))
        {
            BaseAddress = new Uri(hostUrl, UriKind.Absolute)
        };
        return RestService.For<T>(client, settings);
    }

    public T CreateClient<T>(HttpForgeSettings? settings = null)
        where T : class
        => CreateClient<T>("https://stub.local/", settings);

    public Task VerifyAllCalledAsync()
    {
        var missed = _entries.Where(entry => entry.Calls == 0).Select(entry => $"{entry.Route.Method} {entry.Route.Path}").ToList();
        if (missed.Count > 0)
            throw new InvalidOperationException("Stub routes were never called: " + string.Join(", ", missed));

        return Task.CompletedTask;
    }

    IEnumerator IEnumerable.GetEnumerator() => _entries.GetEnumerator();

    internal async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        IHttpContentSerializer serializer,
        CancellationToken cancellationToken)
    {
        _requests.Add(request);
        var entry = _entries.FirstOrDefault(item => item.Route.Matches(request));
        if (entry is null)
            throw new InvalidOperationException($"No stub for {request.Method} {request.RequestUri}");

        entry.Calls++;
        var response = await entry.Reply.ExecuteAsync(request, serializer, cancellationToken).ConfigureAwait(false);
        response.RequestMessage ??= request;
        return response;
    }

    private sealed class StubEntry(Route route, Reply reply)
    {
        public Route Route { get; } = route;
        public Reply Reply { get; } = reply;
        public int Calls { get; set; }
    }

    private sealed class StubHandler(StubHttp owner, HttpForgeSettings? settings) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => owner.SendAsync(request, settings?.ContentSerializer ?? new SystemTextJsonContentSerializer(), cancellationToken);
    }
}
