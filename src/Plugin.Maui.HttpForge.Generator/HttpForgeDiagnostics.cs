using Microsoft.CodeAnalysis;

namespace Plugin.Maui.HttpForge.Generator;

internal static class HttpForgeDiagnostics
{
    public static readonly DiagnosticDescriptor MultipleHttpMethods = new(
        id: "HFG001",
        title: "Multiple HTTP method attributes",
        messageFormat: "Method '{0}' has more than one HttpForge HTTP method attribute",
        category: "HttpForge",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidReturnType = new(
        id: "HFG002",
        title: "Invalid return type",
        messageFormat: "Method '{0}' must return Task, Task<T>, Task<IApiResponse<T>>, or Task<ApiResponse<T>>",
        category: "HttpForge",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MultipleBodies = new(
        id: "HFG003",
        title: "Multiple [Body] parameters",
        messageFormat: "Method '{0}' has more than one [Body] parameter",
        category: "HttpForge",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MultipartWithBody = new(
        id: "HFG004",
        title: "[Multipart] cannot be combined with [Body]",
        messageFormat: "Method '{0}' is [Multipart] and also has a [Body] parameter",
        category: "HttpForge",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingPathParameter = new(
        id: "HFG005",
        title: "Missing path parameter",
        messageFormat: "Method '{0}' route '{1}' references '{{{2}}}' but no matching parameter was found",
        category: "HttpForge",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MultipleCancellationTokens = new(
        id: "HFG006",
        title: "Multiple CancellationToken parameters",
        messageFormat: "Method '{0}' has more than one CancellationToken parameter",
        category: "HttpForge",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
