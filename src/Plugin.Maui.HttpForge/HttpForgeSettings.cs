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

    /// <summary>
    /// Formats query keys (and flattened form keys). Defaults to
    /// <see cref="UrlParameterKeyFormatter.None"/> (names as written).
    /// </summary>
    public UrlParameterKeyFormatter UrlParameterKeyFormatter { get; set; } = UrlParameterKeyFormatter.None;

    /// <summary>
    /// Optional hook that supplies an <c>Authorization</c> header value for each request.
    /// This is not token refresh — compose ApiResilience or SecureSession for 401 retry.
    /// </summary>
    public Func<HttpRequestMessage, CancellationToken, Task<string?>>? AuthorizationHeaderValueGetter { get; set; }

    /// <summary>
    /// Compresses non-multipart request bodies. Defaults to
    /// <see cref="RequestBodyCompression.None"/>. Method-level
    /// <see cref="CompressRequestAttribute"/> overrides this.
    /// </summary>
    public RequestBodyCompression RequestBodyCompression { get; set; } = RequestBodyCompression.None;
}
