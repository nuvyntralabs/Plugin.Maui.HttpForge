using System.Net;
using System.Net.Http.Headers;

namespace Plugin.Maui.HttpForge;

/// <summary>
/// HTTP response plus an optional deserialized body. The caller must dispose the instance.
/// </summary>
public interface IApiResponse<out T> : IDisposable
{
    T? Content { get; }
    HttpStatusCode StatusCode { get; }
    bool IsSuccessStatusCode { get; }
    string? ReasonPhrase { get; }
    HttpResponseHeaders Headers { get; }
    HttpResponseMessage ResponseMessage { get; }
    string? ErrorContent { get; }
    Exception? Error { get; }
}

/// <inheritdoc />
public sealed class ApiResponse<T> : IApiResponse<T>
{
    public ApiResponse(
        HttpResponseMessage response,
        T? content,
        string? errorContent = null,
        Exception? error = null)
    {
        ResponseMessage = response ?? throw new ArgumentNullException(nameof(response));
        Content = content;
        ErrorContent = errorContent;
        Error = error;
    }

    public T? Content { get; }
    public HttpStatusCode StatusCode => ResponseMessage.StatusCode;
    public bool IsSuccessStatusCode => ResponseMessage.IsSuccessStatusCode;
    public string? ReasonPhrase => ResponseMessage.ReasonPhrase;
    public HttpResponseHeaders Headers => ResponseMessage.Headers;
    public HttpResponseMessage ResponseMessage { get; }
    public string? ErrorContent { get; }
    public Exception? Error { get; }

    public void Dispose() => ResponseMessage.Dispose();
}
