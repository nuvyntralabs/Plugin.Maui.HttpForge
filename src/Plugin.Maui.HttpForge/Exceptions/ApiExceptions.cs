using System.Net;
using System.Net.Http.Headers;

namespace Plugin.Maui.HttpForge;

/// <summary>
/// The HTTP request failed after a response was received (4xx / 5xx or a custom factory).
/// </summary>
public class ApiException : Exception
{
    public ApiException(
        string message,
        HttpMethod method,
        Uri? uri,
        HttpStatusCode statusCode,
        string? reasonPhrase,
        string? content,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Method = method;
        Uri = uri;
        StatusCode = statusCode;
        ReasonPhrase = reasonPhrase;
        Content = content;
    }

    public HttpMethod Method { get; }
    public Uri? Uri { get; }
    public HttpStatusCode StatusCode { get; }
    public string? ReasonPhrase { get; }
    public string? Content { get; }

    public static async Task<ApiException> CreateAsync(
        HttpRequestMessage request,
        HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(response);

        var body = response.Content is null
            ? null
            : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        var message = $"{(int)response.StatusCode} ({response.ReasonPhrase}) {request.Method} {RedactUri(request.RequestUri)}";
        return new ApiException(
            message,
            request.Method,
            request.RequestUri,
            response.StatusCode,
            response.ReasonPhrase,
            body);
    }

    internal static string RedactUri(Uri? uri)
    {
        if (uri is null)
            return "";

        if (string.IsNullOrEmpty(uri.UserInfo))
            return uri.ToString();

        var builder = new UriBuilder(uri)
        {
            UserName = "***",
            Password = "***"
        };
        return builder.Uri.ToString();
    }

    internal static void RedactHeaders(HttpRequestHeaders headers)
    {
        foreach (var name in new[] { "Authorization", "Cookie", "Set-Cookie", "X-Api-Key", "X-Auth-Token" })
        {
            if (headers.Contains(name))
            {
                headers.Remove(name);
                headers.TryAddWithoutValidation(name, "***");
            }
        }
    }
}

/// <summary>
/// The HTTP request failed before a response was received (DNS, TLS, timeout, offline).
/// </summary>
public class ApiRequestException : Exception
{
    public ApiRequestException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
