using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NetForge.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ForgePrefixAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "NF0001";
    private static readonly LocalizableString Title = "Type name must be prefixed with Forge";
    private static readonly LocalizableString Message = "Public cross-cutting type '{0}' must start with 'Forge' or 'IForge' (FSA-26)";
    private static readonly LocalizableString Description = "Enforces FSA-26 Forge prefix naming for cross-cutting public types.";
    private const string Category = "Naming";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        Message,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        if (context == null) return; // CA1062 safeguard; Analyzer infrastructure guarantees non-null but rule requests explicit check
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
    }

    private static readonly string[] WatchedNamespaceSuffixes = new[]
    {
        ".Abstractions", ".Results", ".Validation", ".Utilities"
    };

    private static void Analyze(SymbolAnalysisContext ctx)
    {
        if (ctx.Symbol is not INamedTypeSymbol type) return;
        if (type.DeclaredAccessibility != Accessibility.Public) return;
        var ns = type.ContainingNamespace?.ToDisplayString();
        if (string.IsNullOrWhiteSpace(ns)) return;
        if (!WatchedNamespaceSuffixes.Any(s => ns.EndsWith(s, StringComparison.Ordinal))) return;

        var name = type.Name;
        if (name.StartsWith("Forge", StringComparison.Ordinal) || name.StartsWith("IForge", StringComparison.Ordinal)) return;

        if (name.EndsWith("Attribute", StringComparison.Ordinal) || name.EndsWith("Exception", StringComparison.Ordinal)) return;

        var diag = Diagnostic.Create(Rule, type.Locations.FirstOrDefault(), name);
        ctx.ReportDiagnostic(diag);
    }
}
