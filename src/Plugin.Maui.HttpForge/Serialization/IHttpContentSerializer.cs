namespace Plugin.Maui.HttpForge;

/// <summary>
/// Converts request bodies to <see cref="HttpContent"/> and response bodies back to CLR types.
/// </summary>
public interface IHttpContentSerializer
{
    Task<HttpContent> ToHttpContentAsync<T>(T value, CancellationToken cancellationToken = default);

    Task<T?> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken = default);
}
