using System.Reflection;
using NetForge.Core.Results;
using NetForge.Features;

namespace NetForge.Tests.Core.Architecture;

public class ArchitectureRulesTests
{
    private static readonly Assembly Core = typeof(ForgeResult).Assembly;
    private static readonly Assembly Features = typeof(ForgeFeaturesAssemblyMarker).Assembly;

    [Fact]
    public void CorePublicCrossCuttingTypesShouldStartWithForge()
    {
        bool IsPrefixed(Type t) => t.Name.StartsWith("Forge", StringComparison.Ordinal) || t.Name.StartsWith("IForge", StringComparison.Ordinal);

        var violations = Core.GetExportedTypes()
            .Where(t => t.IsClass || t.IsInterface || t.IsValueType)
            .Where(t => t.Namespace != null && (t.Namespace.Contains(".Abstractions", StringComparison.Ordinal) || t.Namespace.Contains(".Results", StringComparison.Ordinal) || t.Namespace.Contains(".Validation", StringComparison.Ordinal) || t.Namespace.Contains(".Utilities", StringComparison.Ordinal)))
            .Where(t => !IsPrefixed(t))
            .Select(t => t.FullName)
            .ToList();

        Assert.True(violations.Count == 0, "Types missing 'Forge' prefix:\n" + string.Join('\n', violations));
    }

    [Fact]
    public void NoLegacyTypeNamesShouldRemain()
    {
        var banned = new HashSet<string> {"Result", "Error", "Mediator", "Guard"};
        var offenders = Core.GetExportedTypes()
            .Where(t => banned.Contains(t.Name))
            .Select(t => t.FullName)
            .ToList();
        Assert.True(offenders.Count == 0, "Legacy type names found:\n" + string.Join('\n', offenders));
    }

    [Fact]
    public void CoreShouldNotDependOnFeaturesInfrastructurePresentation()
    {
        var forbidden = new []{"NetForge.Features", "NetForge.Infrastructure", "NetForge.Presentation"};
        var references = Core.GetReferencedAssemblies().Select(a => a.Name).ToList();
        var hits = references.Where(r => forbidden.Contains(r!)).ToList();
        Assert.True(hits.Count == 0, "Core references forbidden assemblies: " + string.Join(", ", hits));
    }

    [Fact]
    public void FeaturesShouldNotDependOnInfrastructureOrPresentation()
    {
        var forbidden = new []{"NetForge.Infrastructure", "NetForge.Presentation"};
        var references = Features.GetReferencedAssemblies().Select(a => a.Name).ToList();
        var hits = references.Where(r => forbidden.Contains(r!)).ToList();
        Assert.True(hits.Count == 0, "Features references forbidden assemblies: " + string.Join(", ", hits));
    }
}
