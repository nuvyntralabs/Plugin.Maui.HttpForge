using System.IO.Compression;
using System.Net;

namespace Plugin.Maui.HttpForge;

internal sealed class CompressedHttpContent : HttpContent
{
    private readonly HttpContent _inner;
    private readonly RequestBodyCompression _compression;

    public CompressedHttpContent(HttpContent inner, RequestBodyCompression compression)
    {
        _inner = inner;
        _compression = compression;

        foreach (var header in inner.Headers)
            Headers.TryAddWithoutValidation(header.Key, header.Value);

        Headers.ContentEncoding.Clear();
        Headers.ContentEncoding.Add(compression == RequestBodyCompression.Brotli ? "br" : "gzip");
        Headers.ContentLength = null;
    }

    protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        Stream compressed = _compression == RequestBodyCompression.Brotli
            ? new BrotliStream(stream, CompressionLevel.Fastest, leaveOpen: true)
            : new GZipStream(stream, CompressionLevel.Fastest, leaveOpen: true);

        using (compressed)
            _inner.CopyTo(compressed, context, cancellationToken);
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        Stream compressed = _compression == RequestBodyCompression.Brotli
            ? new BrotliStream(stream, CompressionLevel.Fastest, leaveOpen: true)
            : new GZipStream(stream, CompressionLevel.Fastest, leaveOpen: true);

        await using (compressed.ConfigureAwait(false))
            await _inner.CopyToAsync(compressed).ConfigureAwait(false);
    }

    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _inner.Dispose();

        base.Dispose(disposing);
    }
}
