using System.Runtime.CompilerServices;
using Xunit;

namespace SporthalleWeb.Tests.Architecture;

/// <summary>
/// Source-level guards for the vertical-slice layering (see CLAUDE.md,
/// "Architecture: Vertical Slicing"). They scan the main project's source files
/// so a forbidden dependency fails the test suite instead of silently eroding
/// the architecture. Deliberately dependency-free (no NetArchTest/ArchUnit): the
/// rules are simple namespace checks and a source scan keeps the test project's
/// dependency surface minimal.
/// </summary>
public sealed class LayerDependencyTests
{
    private static string ProjectRoot([CallerFilePath] string thisFile = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "SporthalleWeb"));

    private static IEnumerable<string> SourceFiles(string relativeDir)
    {
        var dir = Path.Combine(ProjectRoot(), relativeDir);
        Assert.True(Directory.Exists(dir), $"Expected source directory not found: {dir}");
        return Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(dir, "*.razor", SearchOption.AllDirectories));
    }

    private static List<string> FilesReferencing(string relativeDir, params string[] forbiddenNamespaces)
    {
        var offenders = new List<string>();
        foreach (var file in SourceFiles(relativeDir))
        {
            var text = File.ReadAllText(file);
            foreach (var ns in forbiddenNamespaces)
            {
                if (text.Contains($"using {ns}") || text.Contains($"@using {ns}"))
                {
                    offenders.Add($"{Path.GetFileName(file)} → {ns}");
                    break;
                }
            }
        }
        return offenders;
    }

    [Fact]
    public void Domain_depends_on_nothing_outside_Domain()
    {
        var offenders = FilesReferencing(
            "Domain",
            "SporthalleWeb.Infrastructure",
            "SporthalleWeb.Features",
            "Umbraco",
            "NPoco");

        Assert.True(
            offenders.Count == 0,
            "The Domain layer must not reference frameworks or outer layers. Offenders:\n"
            + string.Join("\n", offenders));
    }

    [Fact]
    public void Features_do_not_reference_Infrastructure_directly()
    {
        var offenders = FilesReferencing("Features", "SporthalleWeb.Infrastructure");

        Assert.True(
            offenders.Count == 0,
            "Features (application layer) must talk to a Port, not an Infrastructure adapter. "
            + "Move the implementation behind a Port in Features/{Feature}/Ports. Offenders:\n"
            + string.Join("\n", offenders));
    }
}
