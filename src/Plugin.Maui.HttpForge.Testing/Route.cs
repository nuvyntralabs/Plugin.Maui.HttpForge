using System.Net.Http;

namespace Plugin.Maui.HttpForge.Testing;

/// <summary>
/// Matches a request method and path. Path tokens such as <c>{id}</c> are wildcards.
/// </summary>
public sealed class Route
{
    public Route(HttpMethod method, string path)
    {
        Method = method ?? throw new ArgumentNullException(nameof(method));
        Path = path ?? throw new ArgumentNullException(nameof(path));
    }

    public HttpMethod Method { get; }

    public string Path { get; }

    public static Route Get(string path) => new(HttpMethod.Get, path);

    public static Route Post(string path) => new(HttpMethod.Post, path);

    public static Route Put(string path) => new(HttpMethod.Put, path);

    public static Route Delete(string path) => new(HttpMethod.Delete, path);

    public static Route Patch(string path) => new(HttpMethod.Patch, path);

    public static Route Head(string path) => new(HttpMethod.Head, path);

    public bool Matches(HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.Method.Equals(Method))
            return false;

        var actual = request.RequestUri is null
            ? "/"
            : request.RequestUri.IsAbsoluteUri
                ? request.RequestUri.AbsolutePath
                : request.RequestUri.OriginalString.Split('?')[0];

        return PathsMatch(Path, actual);
    }

    private static bool PathsMatch(string template, string actual)
    {
        var expected = Split(template);
        var got = Split(actual);
        if (expected.Length != got.Length)
            return false;

        for (var i = 0; i < expected.Length; i++)
        {
            var token = expected[i];
            if (token.StartsWith('{') && token.EndsWith('}'))
                continue;

            if (!string.Equals(token, got[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private static string[] Split(string path)
        => path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
}
