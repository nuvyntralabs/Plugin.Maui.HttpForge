using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Plugin.Maui.HttpForge.Generator;

internal static class ApiInterfaceParser
{
    private static readonly Regex PathToken = new(@"\{([^{}]+)\}", RegexOptions.Compiled);
    private static readonly string[] HttpMethodNames =
    {
        "GetAttribute", "PostAttribute", "PutAttribute", "DeleteAttribute", "PatchAttribute", "HeadAttribute"
    };

    public static ApiInterfaceModel? TryParse(INamedTypeSymbol symbol, Action<Diagnostic> report)
    {
        if (symbol.TypeKind != TypeKind.Interface || symbol.IsGenericType)
            return null;

        var methods = new List<ApiMethodModel>();
        foreach (var member in GetAllMethods(symbol))
        {
            var parsed = TryParseMethod(member, symbol, report);
            if (parsed is not null)
                methods.Add(parsed.Value);
        }

        if (methods.Count == 0)
            return null;

        var ns = symbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : symbol.ContainingNamespace.ToDisplayString();

        return new ApiInterfaceModel(
            Namespace: ns,
            InterfaceName: symbol.Name,
            TypeFullName: symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            HintName: SanitizeHint(symbol.ToDisplayString()),
            InterfaceHeaders: new EquatableArray<string>(ReadHeaders(symbol)),
            Methods: new EquatableArray<ApiMethodModel>(methods));
    }

    private static IEnumerable<IMethodSymbol> GetAllMethods(INamedTypeSymbol symbol)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in new[] { symbol }.Concat(symbol.AllInterfaces))
        {
            foreach (var member in type.GetMembers())
            {
                if (member is not IMethodSymbol method || method.MethodKind != MethodKind.Ordinary)
                    continue;

                var key = method.ToDisplayString();
                if (!seen.Add(key))
                    continue;

                yield return method;
            }
        }
    }

    private static ApiMethodModel? TryParseMethod(IMethodSymbol method, INamedTypeSymbol owner, Action<Diagnostic> report)
    {
        var httpAttrs = method.GetAttributes()
            .Where(IsHttpMethodAttribute)
            .ToImmutableArray();

        if (httpAttrs.Length == 0)
            return null;

        var location = method.Locations.FirstOrDefault() ?? Location.None;

        if (httpAttrs.Length > 1)
        {
            report(Diagnostic.Create(HttpForgeDiagnostics.MultipleHttpMethods, location, method.Name));
            return null;
        }

        var http = httpAttrs[0];
        var httpMethod = ReadHttpMethod(http);
        var path = ReadConstructorString(http) ?? "/";
        var isMultipart = method.GetAttributes().Any(a => a.AttributeClass?.Name == "MultipartAttribute");

        if (!TryGetReturnKind(method.ReturnType, out var returnKind, out var responseType))
        {
            report(Diagnostic.Create(HttpForgeDiagnostics.InvalidReturnType, location, method.Name));
            return null;
        }

        var tokens = PathToken.Matches(path).Cast<Match>().Select(m => m.Groups[1].Value).ToList();
        var parameters = new List<ParameterModel>();
        var bodyCount = 0;
        var cancelCount = 0;

        foreach (var parameter in method.Parameters)
        {
            var parsed = ParseParameter(parameter, tokens, isMultipart);
            if (parsed.Kind == ParameterKinds.Body)
                bodyCount++;
            if (parsed.Kind == ParameterKinds.Cancel)
                cancelCount++;
            parameters.Add(parsed);
        }

        if (bodyCount > 1)
        {
            report(Diagnostic.Create(HttpForgeDiagnostics.MultipleBodies, location, method.Name));
            return null;
        }

        if (isMultipart && bodyCount > 0)
        {
            report(Diagnostic.Create(HttpForgeDiagnostics.MultipartWithBody, location, method.Name));
            return null;
        }

        if (cancelCount > 1)
        {
            report(Diagnostic.Create(HttpForgeDiagnostics.MultipleCancellationTokens, location, method.Name));
            return null;
        }

        var pathNames = new HashSet<string>(
            parameters.Where(p => p.Kind == ParameterKinds.Path).Select(p => p.WireName),
            StringComparer.Ordinal);
        foreach (var token in tokens)
        {
            if (!pathNames.Contains(token))
            {
                report(Diagnostic.Create(HttpForgeDiagnostics.MissingPathParameter, location, method.Name, path, token));
                return null;
            }
        }

        var headers = ReadHeaders(owner)
            .Concat(ReadHeaders(method.ContainingType))
            .Concat(ReadHeaders(method))
            .Distinct()
            .ToList();

        return new ApiMethodModel(
            Name: method.Name,
            HttpMethod: httpMethod,
            Path: path,
            ReturnKind: returnKind,
            ResponseType: responseType,
            IsMultipart: isMultipart,
            Headers: new EquatableArray<string>(headers),
            Parameters: new EquatableArray<ParameterModel>(parameters));
    }

    private static ParameterModel ParseParameter(IParameterSymbol parameter, IReadOnlyList<string> tokens, bool isMultipart)
    {
        var typeName = parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (parameter.NullableAnnotation == NullableAnnotation.Annotated && !typeName.EndsWith("?", StringComparison.Ordinal))
            typeName += "?";
        var alias = ReadAlias(parameter);
        var queryName = ReadQueryName(parameter);
        var headerName = ReadHeaderName(parameter);
        var wireName = queryName ?? alias ?? parameter.Name;
        var hasDefault = parameter.HasExplicitDefaultValue;
        var defaultLiteral = hasDefault ? FormatDefault(parameter) : "default";

        if (IsCancellationToken(parameter.Type))
        {
            return new ParameterModel(parameter.Name, typeName, ParameterKinds.Cancel, wireName, null, hasDefault, defaultLiteral);
        }

        if (headerName is not null)
        {
            return new ParameterModel(parameter.Name, typeName, ParameterKinds.Header, wireName, headerName, hasDefault, defaultLiteral);
        }

        if (HasAttribute(parameter, "BodyAttribute"))
        {
            return new ParameterModel(parameter.Name, typeName, ParameterKinds.Body, wireName, null, hasDefault, defaultLiteral);
        }

        if (tokens.Contains(alias ?? parameter.Name) || tokens.Contains(wireName))
        {
            return new ParameterModel(parameter.Name, typeName, ParameterKinds.Path, alias ?? parameter.Name, null, hasDefault, defaultLiteral);
        }

        if (isMultipart)
        {
            return new ParameterModel(parameter.Name, typeName, ParameterKinds.Multipart, wireName, null, hasDefault, defaultLiteral);
        }

        return new ParameterModel(parameter.Name, typeName, ParameterKinds.Query, wireName, null, hasDefault, defaultLiteral);
    }

    private static bool TryGetReturnKind(ITypeSymbol returnType, out string kind, out string? responseType)
    {
        kind = ReturnKinds.Void;
        responseType = null;

        if (returnType is not INamedTypeSymbol named)
            return false;

        if (named.Name != "Task" || named.ContainingNamespace.ToDisplayString() != "System.Threading.Tasks")
            return false;

        if (!named.IsGenericType)
        {
            kind = ReturnKinds.Void;
            return true;
        }

        var inner = named.TypeArguments[0];
        var innerName = inner.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        if (innerName == "global::System.Net.Http.HttpResponseMessage")
        {
            kind = ReturnKinds.HttpResponse;
            responseType = innerName;
            return true;
        }

        if (inner is INamedTypeSymbol innerNamed &&
            innerNamed.IsGenericType &&
            innerNamed.TypeArguments.Length == 1 &&
            (innerNamed.Name is "IApiResponse" or "ApiResponse") &&
            innerNamed.ContainingNamespace.ToDisplayString() == "Plugin.Maui.HttpForge")
        {
            kind = ReturnKinds.ApiResponse;
            responseType = innerNamed.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return true;
        }

        kind = ReturnKinds.Value;
        responseType = innerName;
        return true;
    }

    private static bool IsHttpMethodAttribute(AttributeData attribute)
    {
        var name = attribute.AttributeClass?.Name;
        if (name is not null && HttpMethodNames.Contains(name))
            return true;

        return attribute.AttributeClass?.BaseType?.ToDisplayString() == "Plugin.Maui.HttpForge.HttpMethodAttribute";
    }

    private static string ReadHttpMethod(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length >= 1 && attribute.ConstructorArguments[0].Value is string method && method.Length <= 7)
        {
            // GetAttribute(string path) — first arg is the path, method comes from the type name.
        }

        return attribute.AttributeClass?.Name switch
        {
            "GetAttribute" => "GET",
            "PostAttribute" => "POST",
            "PutAttribute" => "PUT",
            "DeleteAttribute" => "DELETE",
            "PatchAttribute" => "PATCH",
            "HeadAttribute" => "HEAD",
            _ => "GET"
        };
    }

    private static string? ReadConstructorString(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length == 0)
            return null;

        return attribute.ConstructorArguments[0].Value as string;
    }

    private static IEnumerable<string> ReadHeaders(ISymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.Name != "HeadersAttribute")
                continue;

            foreach (var argument in attribute.ConstructorArguments)
            {
                if (argument.Kind == TypedConstantKind.Array)
                {
                    foreach (var item in argument.Values)
                    {
                        if (item.Value is string header)
                            yield return header;
                    }
                }
                else if (argument.Value is string header)
                {
                    yield return header;
                }
            }
        }
    }

    private static string? ReadAlias(IParameterSymbol parameter)
    {
        var attribute = parameter.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "AliasAsAttribute");
        return attribute?.ConstructorArguments.FirstOrDefault().Value as string;
    }

    private static string? ReadQueryName(IParameterSymbol parameter)
    {
        var attribute = parameter.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "QueryAttribute");
        if (attribute is null)
            return null;

        if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is string name)
            return name;

        return null;
    }

    private static string? ReadHeaderName(IParameterSymbol parameter)
    {
        var attribute = parameter.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "HeaderAttribute");
        return attribute?.ConstructorArguments.FirstOrDefault().Value as string;
    }

    private static bool HasAttribute(IParameterSymbol parameter, string name)
        => parameter.GetAttributes().Any(a => a.AttributeClass?.Name == name);

    private static bool IsCancellationToken(ITypeSymbol type)
        => type.Name == "CancellationToken" && type.ContainingNamespace.ToDisplayString() == "System.Threading";

    private static string FormatDefault(IParameterSymbol parameter)
    {
        if (!parameter.HasExplicitDefaultValue || parameter.ExplicitDefaultValue is null)
            return "default";

        return parameter.ExplicitDefaultValue switch
        {
            string value => "@\"" + value.Replace("\"", "\"\"") + "\"",
            bool value => value ? "true" : "false",
            char value => "'" + value + "'",
            _ => Convert.ToString(parameter.ExplicitDefaultValue, CultureInfo.InvariantCulture) ?? "default"
        };
    }

    private static string SanitizeHint(string value)
    {
        var chars = value.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
        return new string(chars);
    }
}
