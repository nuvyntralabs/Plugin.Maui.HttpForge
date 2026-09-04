namespace Plugin.Maui.HttpForge;

/// <summary>A stream file part for a <see cref="MultipartAttribute"/> request.</summary>
public sealed class StreamPart
{
    public StreamPart(Stream stream, string fileName, string? contentType = null, string? name = null)
    {
        Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        ContentType = contentType;
        Name = name;
    }

    public Stream Stream { get; }
    public string FileName { get; }
    public string? ContentType { get; }
    public string? Name { get; }
}

/// <summary>A byte-array file part for a <see cref="MultipartAttribute"/> request.</summary>
public sealed class ByteArrayPart
{
    public ByteArrayPart(byte[] bytes, string fileName, string? contentType = null, string? name = null)
    {
        Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        ContentType = contentType;
        Name = name;
    }

    public byte[] Bytes { get; }
    public string FileName { get; }
    public string? ContentType { get; }
    public string? Name { get; }
}

/// <summary>A <see cref="FileInfo"/> file part for a <see cref="MultipartAttribute"/> request.</summary>
public sealed class FileInfoPart
{
    public FileInfoPart(FileInfo fileInfo, string? contentType = null, string? name = null)
    {
        FileInfo = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo));
        ContentType = contentType;
        Name = name;
    }

    public FileInfo FileInfo { get; }
    public string? ContentType { get; }
    public string? Name { get; }
}
