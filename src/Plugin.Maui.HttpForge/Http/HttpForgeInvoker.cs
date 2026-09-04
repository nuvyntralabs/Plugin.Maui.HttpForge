using System.Net.Http.Headers;

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
        using var response = await SendCoreAsync(client, request, cancellationToken).ConfigureAwait(false);
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
            var owned = await SendCoreAsync(client, request, cancellationToken).ConfigureAwait(false);
            await EnsureSuccessAsync(request, owned, settings, cancellationToken).ConfigureAwait(false);
            return (T)(object)owned;
        }

        using var response = await SendCoreAsync(client, request, cancellationToken).ConfigureAwait(false);
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
        var response = await SendCoreAsync(client, request, cancellationToken).ConfigureAwait(false);
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

        return Uri.EscapeDataString(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
    }

    private static async Task<HttpResponseMessage> SendCoreAsync(
        HttpClient client,
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(request);

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
