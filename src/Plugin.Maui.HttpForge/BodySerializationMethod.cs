namespace Plugin.Maui.HttpForge;

/// <summary>
/// How a <c>[Body]</c> parameter is written.
/// </summary>
public enum BodySerializationMethod
{
    /// <summary>Use <see cref="HttpForgeSettings.ContentSerializer"/> (JSON by default).</summary>
    Default = 0,

    /// <summary>Same as <see cref="Default"/>.</summary>
    Json = 1,

    /// <summary>Newline-delimited JSON (<c>application/x-ndjson</c>).</summary>
    JsonLines = 2
}
