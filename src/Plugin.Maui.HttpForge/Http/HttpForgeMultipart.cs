using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace Plugin.Maui.HttpForge;

/// <summary>
/// Helpers used by generated multipart methods.
/// </summary>
public static class HttpForgeMultipart
{
    public static void AddPart(MultipartFormDataContent content, string name, object? value)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (value is null)
            return;

        switch (value)
        {
            case StreamPart part:
                AddFile(content, part.Name ?? name, part.Stream, part.FileName, part.ContentType);
                break;
            case ByteArrayPart part:
                AddFile(content, part.Name ?? name, new MemoryStream(part.Bytes, writable: false), part.FileName, part.ContentType);
                break;
            case FileInfoPart part:
                AddFile(content, part.Name ?? name, part.FileInfo.OpenRead(), part.FileInfo.Name, part.ContentType);
                break;
            case Stream stream:
                AddFile(content, name, stream, "file", null);
                break;
            case byte[] bytes:
                AddFile(content, name, new MemoryStream(bytes, writable: false), "file", null);
                break;
            case FileInfo fileInfo:
                AddFile(content, name, fileInfo.OpenRead(), fileInfo.Name, null);
                break;
            case HttpContent httpContent:
                content.Add(httpContent, name);
                break;
            default:
                var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                content.Add(new StringContent(text, Encoding.UTF8), name);
                break;
        }
    }

    private static void AddFile(MultipartFormDataContent content, string name, Stream stream, string fileName, string? contentType)
    {
        var part = new StreamContent(stream);
        if (!string.IsNullOrWhiteSpace(contentType))
            part.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

        content.Add(part, name, fileName);
    }
}
