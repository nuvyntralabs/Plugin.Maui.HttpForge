using Microsoft.Maui.Hosting;

namespace Plugin.Maui.HttpForge;

/// <summary>
/// MAUI host registration for HttpForge.
/// </summary>
public static class MauiAppBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="HttpForgeSettings"/>. Register each API with
    /// <c>builder.Services.AddHttpForgeClient&lt;T&gt;(...)</c>.
    /// </summary>
    public static MauiAppBuilder UseHttpForge(this MauiAppBuilder builder, Action<HttpForgeSettings>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddHttpForge(configure);
        return builder;
    }
}
