using System.Reflection;
using System.Xml.Linq;

namespace OptionsViewer.Tests;

/// <summary>
/// Guarda de arquitectura (RNF-05): el núcleo y la infraestructura no dependen de WinForms ni de ScottPlot.
/// Cada prueba revisa dos cosas:
///  1. los ensamblados referenciados en los metadatos del .dll (solo aparecen los que el código usa);
///  2. los elementos del .csproj (detectan una referencia declarada aunque aún no se use).
/// El .csproj se analiza como XML y se miran solo elementos y atributos concretos, no el texto,
/// para que el nombre del propio proyecto o un comentario no den falsos positivos.
/// </summary>
public class ArchitectureTests
{
    private static readonly string[] ForbiddenAssemblyPrefixes =
        ["System.Windows.Forms", "ScottPlot", "SkiaSharp"];

    private static readonly string[] ForbiddenPackagePrefixes =
        ["ScottPlot", "SkiaSharp", "OpenTK"];

    [Fact]
    public void Core_NoReferenciaWinFormsNiScottPlot()
    {
        AssertNoForbiddenDependencies("OptionsViewer.Core");
    }

    [Fact]
    public void Infrastructure_NoReferenciaWinFormsNiScottPlot()
    {
        AssertNoForbiddenDependencies("OptionsViewer.Infrastructure");
    }

    private static void AssertNoForbiddenDependencies(string projectName)
    {
        var problems = new List<string>();

        // 1. Ensamblados referenciados por el .dll compilado
        var assembly = Assembly.Load(projectName);
        foreach (var name in assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty))
        {
            var prefix = ForbiddenAssemblyPrefixes.FirstOrDefault(
                p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase));
            if (prefix is not null)
                problems.Add($"ensamblado referenciado '{name}' (prefijo prohibido '{prefix}')");
        }

        // 2. Elementos del .csproj
        var path = FindProjectFile(projectName);
        var project = XDocument.Load(path).Root!;

        foreach (var e in project.Descendants("PackageReference"))
        {
            var include = (string?)e.Attribute("Include") ?? string.Empty;
            var prefix = ForbiddenPackagePrefixes.FirstOrDefault(
                p => include.StartsWith(p, StringComparison.OrdinalIgnoreCase));
            if (prefix is not null)
                problems.Add($"<PackageReference Include=\"{include}\"> (prefijo prohibido '{prefix}')");
        }

        foreach (var e in project.Descendants("FrameworkReference"))
            problems.Add($"<FrameworkReference Include=\"{(string?)e.Attribute("Include")}\">");

        foreach (var e in project.Descendants("ProjectReference"))
        {
            var include = (string?)e.Attribute("Include") ?? string.Empty;
            var referenced = Path.GetFileNameWithoutExtension(include.Replace('\\', '/'));
            if (referenced.Equals("OptionsViewer.WinForms", StringComparison.OrdinalIgnoreCase))
                problems.Add($"<ProjectReference Include=\"{include}\"> (apunta al proyecto de interfaz)");
        }

        foreach (var tag in new[] { "TargetFramework", "TargetFrameworks" })
            foreach (var e in project.Descendants(tag))
                if (e.Value.Contains("-windows", StringComparison.OrdinalIgnoreCase))
                    problems.Add($"<{tag}>{e.Value}</{tag}> (TFM específico de Windows)");

        foreach (var tag in new[] { "UseWindowsForms", "UseWPF" })
            foreach (var e in project.Descendants(tag))
                if (e.Value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
                    problems.Add($"<{tag}>true</{tag}>");

        Assert.True(problems.Count == 0,
            $"{projectName} tiene dependencias prohibidas ({Path.GetFileName(path)}):{Environment.NewLine}  - "
            + string.Join($"{Environment.NewLine}  - ", problems));
    }

    private static string FindProjectFile(string projectName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "OptionsViewer.slnx")))
            dir = dir.Parent;

        Assert.True(dir is not null, "No se encontró OptionsViewer.slnx subiendo desde la carpeta de salida.");
        return Path.Combine(dir!.FullName, "src", projectName, projectName + ".csproj");
    }
}
