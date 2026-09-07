namespace Plugin.Maui.HttpForge.Generator;

internal readonly record struct ApiInterfaceModel(
    string Namespace,
    string InterfaceName,
    string TypeFullName,
    string HintName,
    string? PathPrefix,
    string? RequestCompression,
    EquatableArray<string> InterfaceHeaders,
    EquatableArray<ApiMethodModel> Methods);

internal readonly record struct ApiMethodModel(
    string Name,
    string HttpMethod,
    string Path,
    string ReturnKind,
    string? ResponseType,
    bool IsMultipart,
    int TimeoutMilliseconds,
    string? PathPrefix,
    string? RequestCompression,
    EquatableArray<string> Headers,
    EquatableArray<ParameterModel> Parameters);

internal readonly record struct ParameterModel(
    string Name,
    string TypeFullName,
    string Kind,
    string WireName,
    string? HeaderName,
    bool HasDefault,
    string DefaultLiteral,
    string CollectionFormat,
    bool Flatten,
    bool IsOptionalPath,
    string BodySerialization);

internal static class ParameterKinds
{
    public const string Path = "Path";
    public const string Query = "Query";
    public const string Body = "Body";
    public const string Header = "Header";
    public const string Cancel = "Cancel";
    public const string Multipart = "Multipart";
    public const string Url = "Url";
    public const string QueryFlag = "QueryFlag";
    public const string FormObject = "FormObject";
}

internal static class ReturnKinds
{
    public const string Void = "Void";
    public const string Value = "Value";
    public const string ApiResponse = "ApiResponse";
    public const string HttpResponse = "HttpResponse";
    public const string Stream = "Stream";
}
