namespace Plugin.Maui.HttpForge;

/// <summary>
/// Creates a source-generated API client for an interface.
/// </summary>
public static class RestService
{
    public static T For<T>(HttpClient client, HttpForgeSettings? settings = null)
        where T : class
        => HttpForgeClientRegistry.Create<T>(client, settings);

    public static T For<T>(string hostUrl, HttpForgeSettings? settings = null)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostUrl);
        var client = new HttpClient { BaseAddress = new Uri(hostUrl, UriKind.Absolute) };
        return For<T>(client, settings);
    }
}
