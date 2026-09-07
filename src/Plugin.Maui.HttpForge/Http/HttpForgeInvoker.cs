using System.Collections;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;

namespace Plugin.Maui.HttpForge;

/// <summary>
/// Shared send/deserialize path used by generated clients.
/// </summary>
public static class HttpForgeInvoker
{
    public static async Task SendAsync(
        HttpClient client,
        HttpRequestMessage request,
        HttpForgeSettings settings,
        CancellationToken cancellationToken)
    {
        using var response = await SendCoreAsync(client, request, settings, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(request, response, settings, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<T> SendAsync<T>(
        HttpClient client,
        HttpRequestMessage request,
        HttpForgeSettings settings,
        CancellationToken cancellationToken)
    {
        if (typeof(T) == typeof(HttpResponseMessage))
        {
            var owned = await SendCoreAsync(client, request, settings, cancellationToken).ConfigureAwait(false);
            await EnsureSuccessAsync(request, owned, settings, cancellationToken).ConfigureAwait(false);
            return (T)(object)owned;
        }

        using var response = await SendCoreAsync(client, request, settings, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(request, response, settings, cancellationToken).ConfigureAwait(false);

        if (response.Content is null || response.StatusCode == System.Net.HttpStatusCode.NoContent)
            return default!;

        var result = await settings.ContentSerializer.FromHttpContentAsync<T>(response.Content, cancellationToken).ConfigureAwait(false);
        return result!;
    }

    public static async Task<IApiResponse<T>> SendApiResponseAsync<T>(
        HttpClient client,
        HttpRequestMessage request,
        HttpForgeSettings settings,
        CancellationToken cancellationToken)
    {
        var response = await SendCoreAsync(client, request, settings, cancellationToken).ConfigureAwait(false);
        T? content = default;
        string? error = null;
        Exception? errorException = null;

        try
        {
            if (response.IsSuccessStatusCode && response.Content is not null && response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                content = await settings.ContentSerializer.FromHttpContentAsync<T>(response.Content, cancellationToken).ConfigureAwait(false);
            }
            else if (!response.IsSuccessStatusCode && response.Content is not null)
            {
                error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                errorException = await CreateExceptionAsync(request, response, settings, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            errorException = ex;
        }

        return new ApiResponse<T>(response, content, error, errorException);
    }

    public static async IAsyncEnumerable<T> StreamAsync<T>(
        HttpClient client,
        HttpRequestMessage request,
        HttpForgeSettings settings,
        [EnumeratorCancellation] CancellationToken cancellationToken,
        int timeoutMilliseconds = 0)
    {
        using var timeoutCts = timeoutMilliseconds > 0
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : null;
        if (timeoutCts is not null)
            timeoutCts.CancelAfter(timeoutMilliseconds);

        var token = timeoutCts?.Token ?? cancellationToken;
        using var response = await SendCoreAsync(client, request, settings, token).ConfigureAwait(false);
        await EnsureSuccessAsync(request, response, settings, token).ConfigureAwait(false);

        if (response.Content is null)
            yield break;

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        var sse = string.Equals(mediaType, "text/event-stream", StringComparison.OrdinalIgnoreCase);

        await using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        if (sse)
        {
            await foreach (var item in ReadServerSentEventsAsync<T>(reader, settings, token).ConfigureAwait(false))
                yield return item;
        }
        else
        {
            await foreach (var item in ReadJsonLinesAsync<T>(reader, settings, token).ConfigureAwait(false))
                yield return item;
        }
    }

    public static async Task<HttpContent> ToJsonLinesContentAsync(
        IEnumerable values,
        HttpForgeSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(settings);

        var builder = new StringBuilder();
        foreach (var item in values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (item is null)
                continue;

            using var content = await ToContentAsync(item, settings, cancellationToken).ConfigureAwait(false);
            var line = (await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)).TrimEnd('\r', '\n');
            if (line.Length > 0)
                builder.AppendLine(line);
        }

        return new StringContent(builder.ToString(), Encoding.UTF8, "application/x-ndjson");
    }

    public static void ApplyRequestCompression(HttpRequestMessage request, RequestBodyCompression compression)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (compression == RequestBodyCompression.None || request.Content is null)
            return;

        if (request.Content is MultipartFormDataContent || request.Content.Headers.ContentEncoding.Count > 0)
            return;

        request.Content = new CompressedHttpContent(request.Content, compression);
    }

    public static void AddHeader(HttpRequestMessage request, string name, object? value)
    {
        if (string.IsNullOrWhiteSpace(name) || value is null)
            return;

        var text = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
        if (text is null)
            return;

        if (!request.Headers.TryAddWithoutValidation(name, text))
            request.Content?.Headers.TryAddWithoutValidation(name, text);
    }

    public static void AddStaticHeader(HttpRequestMessage request, string header)
    {
        if (string.IsNullOrWhiteSpace(header))
            return;

        var separator = header.IndexOf(':');
        if (separator <= 0)
            return;

        var name = header[..separator].Trim();
        var value = header[(separator + 1)..].Trim();
        AddHeader(request, name, value);
    }

    public static string FormatPathValue(object? value)
    {
        if (value is null)
            return string.Empty;

        return Uri.EscapeDataString(HttpForgeQueryBuilder.FormatValue(value) ?? string.Empty);
    }

    public static string FormatUrl(object? url)
    {
        if (url is null)
            throw new ArgumentNullException(nameof(url));

        if (url is Uri uri)
            return uri.ToString();

        var text = Convert.ToString(url, System.Globalization.CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("The [Url] parameter must be a non-empty string or Uri.", nameof(url));

        return text;
    }

    public static string CombinePath(string? prefix, string path)
    {
        path ??= "/";
        if (string.IsNullOrWhiteSpace(prefix))
            return path;

        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        var trimmedPrefix = prefix.TrimEnd('/');
        if (trimmedPrefix.Length == 0)
            return path.StartsWith('/') ? path : "/" + path;

        var rest = path.StartsWith('/') ? path : "/" + path;
        return trimmedPrefix + rest;
    }

    private static async Task<HttpResponseMessage> SendCoreAsync(
        HttpClient client,
        HttpRequestMessage request,
        HttpForgeSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(settings);

        EnsureAbsoluteRequestUri(client, request);
        await ApplyAuthorizationAsync(request, settings, cancellationToken).ConfigureAwait(false);

        try
        {
            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ApiRequestException("The HTTP request failed before a response was received.", ex);
        }
    }

    private static void EnsureAbsoluteRequestUri(HttpClient client, HttpRequestMessage request)
    {
        if (request.RequestUri is null || request.RequestUri.IsAbsoluteUri || client.BaseAddress is null)
            return;

        request.RequestUri = new Uri(client.BaseAddress, request.RequestUri);
    }

    private static async Task ApplyAuthorizationAsync(
        HttpRequestMessage request,
        HttpForgeSettings settings,
        CancellationToken cancellationToken)
    {
        if (settings.AuthorizationHeaderValueGetter is null)
            return;

        var value = await settings.AuthorizationHeaderValueGetter(request, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(value))
            return;

        request.Headers.Remove("Authorization");
        request.Headers.TryAddWithoutValidation("Authorization", value);
    }

    private static async IAsyncEnumerable<T> ReadJsonLinesAsync<T>(
        StreamReader reader,
        HttpForgeSettings settings,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
                yield break;

            line = line.Trim();
            if (line.Length == 0)
                continue;

            var item = await DeserializeLineAsync<T>(line, settings, cancellationToken).ConfigureAwait(false);
            if (item is not null)
                yield return item;
        }
    }

    private static async IAsyncEnumerable<T> ReadServerSentEventsAsync<T>(
        StreamReader reader,
        HttpForgeSettings settings,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var data = new StringBuilder();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                if (TryTakeSseData(data, out var trailing))
                {
                    var item = await DeserializeLineAsync<T>(trailing, settings, cancellationToken).ConfigureAwait(false);
                    if (item is not null)
                        yield return item;
                }

                yield break;
            }

            if (line.Length == 0)
            {
                if (TryTakeSseData(data, out var payload))
                {
                    var item = await DeserializeLineAsync<T>(payload, settings, cancellationToken).ConfigureAwait(false);
                    if (item is not null)
                        yield return item;
                }

                continue;
            }

            if (line[0] == ':')
                continue;

            if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                if (data.Length > 0)
                    data.Append('\n');
                data.Append(line.AsSpan(5).TrimStart());
            }
        }
    }

    private static bool TryTakeSseData(StringBuilder data, out string payload)
    {
        payload = data.ToString();
        data.Clear();
        return payload.Length > 0;
    }

    private static async Task<T?> DeserializeLineAsync<T>(string line, HttpForgeSettings settings, CancellationToken cancellationToken)
    {
        using var content = new StringContent(line, Encoding.UTF8, "application/json");
        return await settings.ContentSerializer.FromHttpContentAsync<T>(content, cancellationToken).ConfigureAwait(false);
    }

    private static Task<HttpContent> ToContentAsync(object value, HttpForgeSettings settings, CancellationToken cancellationToken)
        => settings.ContentSerializer.ToHttpContentAsync(value, cancellationToken);

    private static async Task EnsureSuccessAsync(
        HttpRequestMessage request,
        HttpResponseMessage response,
        HttpForgeSettings settings,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var exception = await CreateExceptionAsync(request, response, settings, cancellationToken).ConfigureAwait(false);
        if (exception is not null)
            throw exception;
    }

    private static async Task<Exception?> CreateExceptionAsync(
        HttpRequestMessage request,
        HttpResponseMessage response,
        HttpForgeSettings settings,
        CancellationToken cancellationToken)
    {
        if (settings.ExceptionFactory is not null)
            return await settings.ExceptionFactory(response, cancellationToken).ConfigureAwait(false);

        return await ApiException.CreateAsync(request, response, cancellationToken).ConfigureAwait(false);
    }

    internal static void RedactSensitiveHeaders(HttpRequestHeaders headers) => ApiException.RedactHeaders(headers);
}
