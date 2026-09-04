namespace Plugin.Maui.HttpForge;

/// <summary>
/// Runtime settings for generated HttpForge clients.
/// </summary>
public sealed class HttpForgeSettings
{
    /// <summary>
    /// Serializer used for JSON (and other) request/response bodies.
    /// Defaults to <see cref="SystemTextJsonContentSerializer"/>.
    /// </summary>
    public IHttpContentSerializer ContentSerializer { get; set; } = new SystemTextJsonContentSerializer();

    /// <summary>
    /// Optional factory that maps a failed HTTP response to an exception.
    /// Return <c>null</c> to suppress the exception. Transport failures always become
    /// <see cref="ApiRequestException"/>.
    /// </summary>
    public Func<HttpResponseMessage, CancellationToken, Task<Exception?>>? ExceptionFactory { get; set; }
}
