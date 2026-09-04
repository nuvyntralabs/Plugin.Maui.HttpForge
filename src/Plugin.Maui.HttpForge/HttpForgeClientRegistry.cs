using System.Collections.Concurrent;

namespace Plugin.Maui.HttpForge;

/// <summary>
/// Maps API interfaces to the source-generated implementation. Generated code registers factories
/// with <see cref="System.Runtime.CompilerServices.ModuleInitializerAttribute"/>.
/// </summary>
public static class HttpForgeClientRegistry
{
    private static readonly ConcurrentDictionary<Type, Func<HttpClient, HttpForgeSettings, object>> Factories = new();

    public static void Register<TClient>(Func<HttpClient, HttpForgeSettings, TClient> factory)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        Factories[typeof(TClient)] = (client, settings) => factory(client, settings);
    }

    public static bool IsRegistered<TClient>() where TClient : class
        => Factories.ContainsKey(typeof(TClient));

    public static TClient Create<TClient>(HttpClient client, HttpForgeSettings? settings = null)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(client);

        if (!Factories.TryGetValue(typeof(TClient), out var factory))
        {
            throw new InvalidOperationException(
                $"No HttpForge client was generated for {typeof(TClient)}. " +
                "Add [Get]/[Post]/[Put]/[Delete]/[Patch]/[Head] to the interface methods and rebuild so the source generator can run.");
        }

        return (TClient)factory(client, settings ?? new HttpForgeSettings());
    }
}
