using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace Plugin.Maui.HttpForge;

/// <summary>
/// Dependency injection helpers. The returned <see cref="IHttpClientBuilder"/> can be chained
/// with sibling plugins such as <c>AddApiResilience()</c>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers default <see cref="HttpForgeSettings"/>.
    /// </summary>
    public static IServiceCollection AddHttpForge(this IServiceCollection services, Action<HttpForgeSettings>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<HttpForgeSettings>();
        if (configure is not null)
            services.Configure(configure);

        services.AddSingleton(sp =>
        {
            var options = sp.GetService<IOptions<HttpForgeSettings>>();
            return options?.Value ?? new HttpForgeSettings();
        });

        return services;
    }

    /// <summary>
    /// Registers a typed HttpForge client. Compose resilience or cache on the returned builder:
    /// <c>services.AddHttpForgeClient&lt;IUserApi&gt;(c =&gt; c.BaseAddress = ...).AddApiResilience()</c>.
    /// </summary>
    public static IHttpClientBuilder AddHttpForgeClient<TClient>(
        this IServiceCollection services,
        Action<HttpClient>? configureClient = null)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddHttpForge();

        var builder = services.AddHttpClient(GetClientName<TClient>());
        if (configureClient is not null)
            builder.ConfigureHttpClient(configureClient);

        builder.AddTypedClient<TClient>((http, sp) =>
        {
            var settings = sp.GetService<HttpForgeSettings>() ?? new HttpForgeSettings();
            return RestService.For<TClient>(http, settings);
        });

        return builder;
    }

    /// <summary>
    /// Registers a typed HttpForge client with host-aware <see cref="HttpClient"/> configuration.
    /// </summary>
    public static IHttpClientBuilder AddHttpForgeClient<TClient>(
        this IServiceCollection services,
        Action<IServiceProvider, HttpClient> configureClient)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureClient);
        services.AddHttpForge();

        var builder = services.AddHttpClient(GetClientName<TClient>());
        builder.ConfigureHttpClient(configureClient);
        builder.AddTypedClient<TClient>((http, sp) =>
        {
            var settings = sp.GetService<HttpForgeSettings>() ?? new HttpForgeSettings();
            return RestService.For<TClient>(http, settings);
        });

        return builder;
    }

    public static string GetClientName<TClient>() where TClient : class
        => typeof(TClient).FullName ?? typeof(TClient).Name;
}
