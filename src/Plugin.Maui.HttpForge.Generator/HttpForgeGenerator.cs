using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Plugin.Maui.HttpForge.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class HttpForgeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var interfaces = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is InterfaceDeclarationSyntax,
                static (ctx, _) => ctx.SemanticModel.GetDeclaredSymbol(ctx.Node) as INamedTypeSymbol)
            .Where(static symbol => symbol is not null)
            .Select(static (symbol, _) => symbol!);

        var parsed = interfaces.Select(static (symbol, _) =>
        {
            var diagnostics = new List<Diagnostic>();
            var model = ApiInterfaceParser.TryParse(symbol, diagnostics.Add);
            return (Model: model, Diagnostics: new EquatableArray<DiagnosticKey>(diagnostics.Select(DiagnosticKey.From)));
        });

        context.RegisterSourceOutput(parsed, static (production, item) =>
        {
            foreach (var diagnostic in item.Diagnostics.AsImmutableArray())
                production.ReportDiagnostic(diagnostic.ToDiagnostic());

            if (item.Model is { } model)
            {
                production.AddSource(model.HintName + ".HttpForge.g.cs", HttpForgeEmitter.Emit(model));
            }
        });
    }
}

internal readonly record struct DiagnosticKey(
    string Id,
    string Title,
    string Message,
    string Category,
    int Severity,
    string Path,
    int Start,
    int Length)
{
    public static DiagnosticKey From(Diagnostic diagnostic)
    {
        var location = diagnostic.Location;
        var span = location.SourceSpan;
        return new DiagnosticKey(
            diagnostic.Id,
            diagnostic.Descriptor.Title.ToString(),
            diagnostic.GetMessage(),
            diagnostic.Descriptor.Category,
            (int)diagnostic.Severity,
            location.SourceTree?.FilePath ?? string.Empty,
            span.Start,
            span.Length);
    }

    public Diagnostic ToDiagnostic()
    {
        var descriptor = new DiagnosticDescriptor(
            Id,
            Title,
            "{0}",
            Category,
            (DiagnosticSeverity)Severity,
            isEnabledByDefault: true);

        return Diagnostic.Create(descriptor, Location.None, Message);
    }
}
