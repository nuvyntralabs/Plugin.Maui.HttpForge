using System.Text;

namespace Plugin.Maui.HttpForge;

/// <summary>
/// Formats query (and form) keys. Assign a static instance to
/// <see cref="HttpForgeSettings.UrlParameterKeyFormatter"/>.
/// </summary>
public sealed class UrlParameterKeyFormatter
{
    public static UrlParameterKeyFormatter None { get; } = new(UrlParameterKeyFormat.None);

    public static UrlParameterKeyFormatter CamelCase { get; } = new(UrlParameterKeyFormat.CamelCase);

    public static UrlParameterKeyFormatter SnakeCase { get; } = new(UrlParameterKeyFormat.SnakeCase);

    public static UrlParameterKeyFormatter KebabCase { get; } = new(UrlParameterKeyFormat.KebabCase);

    private readonly UrlParameterKeyFormat _format;

    public UrlParameterKeyFormatter(UrlParameterKeyFormat format)
    {
        _format = format;
    }

    public string Format(string key)
    {
        if (string.IsNullOrEmpty(key) || _format == UrlParameterKeyFormat.None)
            return key;

        return _format switch
        {
            UrlParameterKeyFormat.CamelCase => ToCamelCase(key),
            UrlParameterKeyFormat.SnakeCase => ToSeparated(key, '_'),
            UrlParameterKeyFormat.KebabCase => ToSeparated(key, '-'),
            _ => key
        };
    }

    private static string ToCamelCase(string key)
    {
        if (key.IndexOfAny(['_', '-']) >= 0)
        {
            var parts = key.Split(['_', '-'], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return key;

            var builder = new StringBuilder(key.Length);
            builder.Append(parts[0].Length == 0
                ? parts[0]
                : char.ToLowerInvariant(parts[0][0]) + parts[0][1..]);
            for (var i = 1; i < parts.Length; i++)
            {
                if (parts[i].Length == 0)
                    continue;
                builder.Append(char.ToUpperInvariant(parts[i][0]));
                if (parts[i].Length > 1)
                    builder.Append(parts[i].AsSpan(1));
            }

            return builder.ToString();
        }

        if (char.IsUpper(key[0]))
            return char.ToLowerInvariant(key[0]) + key[1..];

        return key;
    }

    private static string ToSeparated(string key, char separator)
    {
        var builder = new StringBuilder(key.Length + 4);
        for (var i = 0; i < key.Length; i++)
        {
            var ch = key[i];
            if (ch is '_' or '-')
            {
                if (builder.Length > 0 && builder[^1] != separator)
                    builder.Append(separator);
                continue;
            }

            if (char.IsUpper(ch) && i > 0 && builder.Length > 0 && builder[^1] != separator)
                builder.Append(separator);

            builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.ToString();
    }
}

/// <summary>Naming policy applied by <see cref="UrlParameterKeyFormatter"/>.</summary>
public enum UrlParameterKeyFormat
{
    None = 0,
    CamelCase = 1,
    SnakeCase = 2,
    KebabCase = 3
}
