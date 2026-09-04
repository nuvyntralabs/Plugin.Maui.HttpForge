using System.Globalization;
using System.Text;

namespace Plugin.Maui.HttpForge;

/// <summary>
/// Builds a query string for generated clients. Null values are omitted.
/// </summary>
public sealed class HttpForgeQueryBuilder
{
    private readonly List<string> _parts = [];

    public HttpForgeQueryBuilder Add(string name, object? value)
    {
        if (value is null)
            return this;

        var text = Convert.ToString(value, CultureInfo.InvariantCulture);
        if (text is null)
            return this;

        _parts.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(text)}");
        return this;
    }

    public override string ToString()
    {
        if (_parts.Count == 0)
            return string.Empty;

        var builder = new StringBuilder();
        builder.Append('?');
        for (var i = 0; i < _parts.Count; i++)
        {
            if (i > 0)
                builder.Append('&');
            builder.Append(_parts[i]);
        }

        return builder.ToString();
    }
}
