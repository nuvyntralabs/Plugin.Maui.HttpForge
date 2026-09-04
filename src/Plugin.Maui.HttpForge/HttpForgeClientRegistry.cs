using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Plugin.Maui.HttpForge;

/// <summary>
/// Maps API interfaces to the source-generated implementation. Generated code registers factories
/// with <see cref="System.Runtime.CompilerServices.ModuleInitializerAttribute"/> and also emits
/// <see cref="HttpForgeClientAttribute"/> so Android/ILLink can still discover the client.
/// </summary>
public static class HttpForgeClientRegistry
{
    private static readonly ConcurrentDictionary<Type, Func<HttpClient, HttpForgeSettings, object>> Factories = new();
    private static readonly ConcurrentDictionary<Assembly, byte> Discovered = new();

    public static void Register<TClient>(Func<HttpClient, HttpForgeSettings, TClient> factory)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        Factories[typeof(TClient)] = (client, settings) => factory(client, settings);
    }

    public static bool IsRegistered<TClient>() where TClient : class
    {
        Discover(typeof(TClient).Assembly);
        return Factories.ContainsKey(typeof(TClient));
    }

    public static TClient Create<TClient>(HttpClient client, HttpForgeSettings? settings = null)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(client);
        Discover(typeof(TClient).Assembly);

        if (!Factories.TryGetValue(typeof(TClient), out var factory))
        {
            throw new InvalidOperationException(
                $"No HttpForge client was generated for {typeof(TClient)}. " +
                "Add [Get]/[Post]/[Put]/[Delete]/[Patch]/[Head] to the interface methods and rebuild so the source generator can run.");
        }

        return (TClient)factory(client, settings ?? new HttpForgeSettings());
    }

    private static void Discover(Assembly assembly)
    {
        if (!Discovered.TryAdd(assembly, 0))
            return;

        foreach (var attribute in assembly.GetCustomAttributes<HttpForgeClientAttribute>())
        {
            var contract = attribute.Contract;
            var implementation = attribute.Implementation;
            Factories.TryAdd(contract, (http, settings) => CreateInstance(implementation, http, settings));
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Implementation type is preserved by HttpForgeClientAttribute.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Implementation type is preserved by HttpForgeClientAttribute.")]
    private static object CreateInstance(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementation,
        HttpClient client,
        HttpForgeSettings settings)
        => Activator.CreateInstance(implementation, client, settings)
           ?? throw new InvalidOperationException($"Failed to create {implementation}.");
}
