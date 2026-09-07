using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection;
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

    public static void AddFormObject(
        MultipartFormDataContent content,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] object? value,
        UrlParameterKeyFormatter? formatter = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (value is null)
            return;

        AddFormObjectCore(content, value, prefix: null, formatter ?? UrlParameterKeyFormatter.None);
    }

    private static void AddFormObjectCore(
        MultipartFormDataContent content,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] object value,
        string? prefix,
        UrlParameterKeyFormatter formatter)
    {
        foreach (var property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
                continue;

            var propertyValue = property.GetValue(value);
            if (propertyValue is null)
                continue;

            var rawName = prefix is null ? property.Name : prefix + "." + property.Name;
            var name = formatter.Format(rawName);

            if (propertyValue is IEnumerable enumerable and not string)
            {
                foreach (var item in enumerable)
                {
                    if (item is not null)
                        AddPart(content, name, item);
                }
            }
            else if (IsSimple(propertyValue) || propertyValue is StreamPart or ByteArrayPart or FileInfoPart or Stream or byte[] or FileInfo or HttpContent)
            {
                AddPart(content, name, propertyValue);
            }
            else
            {
                AddFormObjectCore(content, propertyValue, rawName, formatter);
            }
        }
    }

    private static bool IsSimple(object value)
    {
        var type = value.GetType();
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive
               || type.IsEnum
               || type == typeof(string)
               || type == typeof(decimal)
               || type == typeof(DateTime)
               || type == typeof(DateTimeOffset)
               || type == typeof(Guid)
               || type == typeof(TimeSpan)
               || type == typeof(Uri);
    }

    private static void AddFile(MultipartFormDataContent content, string name, Stream stream, string fileName, string? contentType)
    {
        var part = new StreamContent(stream);
        if (!string.IsNullOrWhiteSpace(contentType))
            part.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

        content.Add(part, name, fileName);
    }
}
