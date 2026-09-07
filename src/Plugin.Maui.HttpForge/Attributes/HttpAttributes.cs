namespace Plugin.Maui.HttpForge;

/// <summary>
/// Base attribute for HTTP methods declared on an API interface.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public abstract class HttpMethodAttribute : Attribute
{
    /// <summary>HTTP verb, for example <c>GET</c>.</summary>
    public string Method { get; }

    /// <summary>Relative or absolute route template, for example <c>/users/{id}</c>.</summary>
    public string Path { get; }

    protected HttpMethodAttribute(string method, string path)
    {
        Method = method ?? throw new ArgumentNullException(nameof(method));
        Path = path ?? throw new ArgumentNullException(nameof(path));
    }
}

/// <summary>Maps the method to HTTP GET.</summary>
public sealed class GetAttribute : HttpMethodAttribute
{
    public GetAttribute(string path) : base("GET", path)
    {
    }
}

/// <summary>Maps the method to HTTP POST.</summary>
public sealed class PostAttribute : HttpMethodAttribute
{
    public PostAttribute(string path) : base("POST", path)
    {
    }
}

/// <summary>Maps the method to HTTP PUT.</summary>
public sealed class PutAttribute : HttpMethodAttribute
{
    public PutAttribute(string path) : base("PUT", path)
    {
    }
}

/// <summary>Maps the method to HTTP DELETE.</summary>
public sealed class DeleteAttribute : HttpMethodAttribute
{
    public DeleteAttribute(string path) : base("DELETE", path)
    {
    }
}

/// <summary>Maps the method to HTTP PATCH.</summary>
public sealed class PatchAttribute : HttpMethodAttribute
{
    public PatchAttribute(string path) : base("PATCH", path)
    {
    }
}

/// <summary>Maps the method to HTTP HEAD.</summary>
public sealed class HeadAttribute : HttpMethodAttribute
{
    public HeadAttribute(string path) : base("HEAD", path)
    {
    }
}

/// <summary>Marks a parameter as the HTTP request body.</summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class BodyAttribute : Attribute
{
    public BodyAttribute()
    {
    }

    public BodyAttribute(BodySerializationMethod serializationMethod)
    {
        SerializationMethod = serializationMethod;
    }

    /// <summary>Defaults to <see cref="BodySerializationMethod.Default"/> (JSON).</summary>
    public BodySerializationMethod SerializationMethod { get; set; } = BodySerializationMethod.Default;
}

/// <summary>Marks a parameter as a query string value, collection, or flattened object.</summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class QueryAttribute : Attribute
{
    public QueryAttribute()
    {
    }

    public QueryAttribute(string name)
    {
        Name = name;
    }

    public QueryAttribute(CollectionFormat collectionFormat)
    {
        CollectionFormat = collectionFormat;
    }

    public QueryAttribute(string name, CollectionFormat collectionFormat)
    {
        Name = name;
        CollectionFormat = collectionFormat;
    }

    /// <summary>Query key. Defaults to the parameter name or <see cref="AliasAsAttribute"/>.</summary>
    public string? Name { get; }

    /// <summary>How collection values are written. Defaults to <see cref="CollectionFormat.Multi"/>.</summary>
    public CollectionFormat CollectionFormat { get; set; } = CollectionFormat.Multi;
}

/// <summary>Sends the parameter as a request header.</summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class HeaderAttribute : Attribute
{
    public HeaderAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public string Name { get; }
}

/// <summary>Adds static headers to every call on the interface or method.</summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HeadersAttribute : Attribute
{
    public HeadersAttribute(params string[] headers)
    {
        Headers = headers ?? throw new ArgumentNullException(nameof(headers));
    }

    /// <summary>Entries in <c>Name: Value</c> form.</summary>
    public string[] Headers { get; }
}

/// <summary>Overrides the wire name of a path, query, header, or multipart parameter.</summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class AliasAsAttribute : Attribute
{
    public AliasAsAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public string Name { get; }
}

/// <summary>Sends the request as <c>multipart/form-data</c>.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class MultipartAttribute : Attribute
{
}

/// <summary>
/// Cancels this call after the given milliseconds. App-wide timeouts stay on
/// <see cref="HttpClient"/> or ApiResilience.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TimeoutAttribute : Attribute
{
    public TimeoutAttribute(int milliseconds)
    {
        Milliseconds = milliseconds;
    }

    public int Milliseconds { get; }
}

/// <summary>
/// Replaces the method path with this parameter (string or <see cref="Uri"/>).
/// A runtime URL changes the trust boundary — validate the value before calling
/// (SSRF risk if the host accepts untrusted input).
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class UrlAttribute : Attribute
{
}

/// <summary>Prepends a shared prefix to every method path on the interface.</summary>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class PathPrefixAttribute : Attribute
{
    public PathPrefixAttribute(string prefix)
    {
        Prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
    }

    public string Prefix { get; }
}

/// <summary>
/// Writes a valueless query flag. A <c>string</c> value becomes the flag name
/// (<c>?archived</c>). A <c>bool</c> uses the parameter (or alias) name when true.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class QueryNameAttribute : Attribute
{
    public QueryNameAttribute()
    {
    }

    public QueryNameAttribute(string name)
    {
        Name = name;
    }

    /// <summary>Flag name when the parameter is <c>bool</c>. Defaults to the parameter name.</summary>
    public string? Name { get; }
}

/// <summary>
/// Flattens an object into multipart form fields (<c>name</c>, <c>address.city</c>).
/// Requires <see cref="MultipartAttribute"/> on the method.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FormObjectAttribute : Attribute
{
}

/// <summary>
/// Gzip- or Brotli-encodes the request body and sets <c>Content-Encoding</c>.
/// Skipped for multipart and already-encoded content. Not the default.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface)]
public sealed class CompressRequestAttribute : Attribute
{
    public CompressRequestAttribute(RequestBodyCompression compression = RequestBodyCompression.Gzip)
    {
        Compression = compression;
    }

    public RequestBodyCompression Compression { get; }
}
