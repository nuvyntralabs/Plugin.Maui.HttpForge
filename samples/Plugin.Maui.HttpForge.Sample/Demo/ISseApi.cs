using Plugin.Maui.HttpForge;

namespace Plugin.Maui.HttpForge.Sample.Demo;

public sealed class SseEvent
{
    public string? Type { get; set; }
    public string? Title { get; set; }
    public string? Wiki { get; set; }
    public string? User { get; set; }
}

/// <summary>
/// Wikimedia EventStreams — public <c>text/event-stream</c> that stays up.
/// </summary>
public interface ISseApi
{
    [Get("/v2/stream/recentchange")]
    IAsyncEnumerable<SseEvent> StreamAsync(CancellationToken cancellationToken = default);
}
