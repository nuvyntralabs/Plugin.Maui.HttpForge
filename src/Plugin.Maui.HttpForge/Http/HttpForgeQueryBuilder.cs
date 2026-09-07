using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Plugin.Maui.HttpForge;

/// <summary>
/// Builds a query string for generated clients. Null values are omitted.
/// </summary>
public sealed class HttpForgeQueryBuilder
{
    private readonly List<string> _parts = [];
    private readonly UrlParameterKeyFormatter _formatter;

    public HttpForgeQueryBuilder()
        : this(null)
    {
    }

    public HttpForgeQueryBuilder(HttpForgeSettings? settings)
    {
        _formatter = settings?.UrlParameterKeyFormatter ?? UrlParameterKeyFormatter.None;
    }

    public HttpForgeQueryBuilder Add(string name, object? value)
        => Add(name, value, CollectionFormat.Multi);

    public HttpForgeQueryBuilder Add(string name, object? value, CollectionFormat format)
    {
        if (value is null)
            return this;

        if (IsCollection(value))
            return AddCollection(name, (IEnumerable)value, format);

        var text = FormatValue(value);
        if (text is null)
            return this;

        _parts.Add($"{Escape(_formatter.Format(name))}={Escape(text)}");
        return this;
    }

    public HttpForgeQueryBuilder AddCollection(string name, IEnumerable? values, CollectionFormat format = CollectionFormat.Multi)
    {
        if (values is null)
            return this;

        var items = new List<string>();
        foreach (var item in values)
        {
            if (item is null)
                continue;

            var text = FormatValue(item);
            if (text is not null)
                items.Add(text);
        }

        if (items.Count == 0)
            return this;

        var key = Escape(_formatter.Format(name));
        switch (format)
        {
            case CollectionFormat.Csv:
                _parts.Add($"{key}={Escape(string.Join(",", items))}");
                break;
            case CollectionFormat.Ssv:
                _parts.Add($"{key}={Escape(string.Join(" ", items))}");
                break;
            case CollectionFormat.Tsv:
                _parts.Add($"{key}={Escape(string.Join("\t", items))}");
                break;
            case CollectionFormat.Pipes:
                _parts.Add($"{key}={Escape(string.Join("|", items))}");
                break;
            default:
                foreach (var item in items)
                    _parts.Add($"{key}={Escape(item)}");
                break;
        }

        return this;
    }

    public HttpForgeQueryBuilder AddObject(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] object? value,
        CollectionFormat format = CollectionFormat.Multi)
    {
        if (value is null)
            return this;

        foreach (var property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
                continue;

            var propertyValue = property.GetValue(value);
            if (propertyValue is null)
                continue;

            if (IsCollection(propertyValue))
                AddCollection(property.Name, (IEnumerable)propertyValue, format);
            else if (IsSimple(propertyValue))
                Add(property.Name, propertyValue);
            else
                FlattenObject(property.Name, propertyValue, format);
        }

        return this;
    }

    public HttpForgeQueryBuilder AddFlag(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return this;

        _parts.Add(Escape(_formatter.Format(name)));
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

    private void FlattenObject(
        string prefix,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] object value,
        CollectionFormat format)
    {
        foreach (var property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
                continue;

            var propertyValue = property.GetValue(value);
            if (propertyValue is null)
                continue;

            var key = prefix + "." + property.Name;
            if (IsCollection(propertyValue))
                AddCollection(key, (IEnumerable)propertyValue, format);
            else if (IsSimple(propertyValue))
                Add(key, propertyValue);
            else
                FlattenObject(key, propertyValue, format);
        }
    }

    internal static string? FormatValue(object value) => value switch
    {
        DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
        bool flag => flag ? "true" : "false",
        _ => Convert.ToString(value, CultureInfo.InvariantCulture)
    };

    private static bool IsCollection(object value) => value is IEnumerable and not string;

    private static bool IsSimple(object value)
    {
        var type = value.GetType();
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive
               || type.IsEnum
               || type == typeof(string)
               || type == typeof(decimal)
               || type == typeof(DateTime)
               || type == typeof(DateTimeOffset)
               || type == typeof(Guid)
               || type == typeof(TimeSpan)
               || type == typeof(Uri);
    }

    private static string Escape(string value) => Uri.EscapeDataString(value);
}
