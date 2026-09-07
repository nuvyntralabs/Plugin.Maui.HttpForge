namespace Plugin.Maui.HttpForge;

/// <summary>
/// Compresses a JSON (or other non-multipart) request body and sets <c>Content-Encoding</c>.
/// </summary>
public enum RequestBodyCompression
{
    None = 0,
    Gzip = 1,
    Brotli = 2
}
