using System.Reflection;

namespace OptionsViewer.Tests;

/// <summary>
/// CA-11: hay pruebas automáticas, sin interfaz, que cubren al menos CA-01, CA-03, CA-04, CA-05, CA-09 y CA-13.
/// Esta prueba recorre las pruebas del ensamblado y comprueba que cada uno de esos criterios tiene al menos una
/// prueba activa (ni omitida ni explícita) marcada con <c>[Trait("CA", "CA-xx")]</c>.
/// </summary>
public class RequiredCriteriaTests
{
    private static readonly string[] RequiredCriteria = ["CA-01", "CA-03", "CA-04", "CA-05", "CA-09", "CA-13"];

    [Fact]
    [Trait("CA", "CA-11")]
    public void CriteriosObligatorios_TienenPruebasAutomaticas()
    {
        var testMethods = typeof(RequiredCriteriaTests).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(IsActiveTest)
            .ToList();

        // criterio -> pruebas activas que llevan ese rasgo (en el método o en su clase)
        var testsByCriterion = RequiredCriteria.ToDictionary(
            criterion => criterion,
            criterion => testMethods.Where(m => CriteriaOf(m).Contains(criterion)).Select(m => $"{m.DeclaringType!.Name}.{m.Name}").ToList());

        var missing = RequiredCriteria.Where(c => testsByCriterion[c].Count == 0).ToList();

        Assert.True(
            missing.Count == 0,
            $"Faltan pruebas automáticas con [Trait(\"CA\", ...)] para: {string.Join(", ", missing)}. "
            + $"CA-11 exige al menos una para cada uno de: {string.Join(", ", RequiredCriteria)}.");
    }

    private static bool IsActiveTest(MethodInfo method) =>
        method.GetCustomAttributes<FactAttribute>(inherit: true).Any(f => f.Skip is null && !f.Explicit);

    private static IEnumerable<string> CriteriaOf(MethodInfo method) =>
        method.GetCustomAttributes<TraitAttribute>(inherit: true)
            .Concat(method.DeclaringType!.GetCustomAttributes<TraitAttribute>(inherit: true))
            .Where(t => t.Name == "CA")
            .Select(t => t.Value);
}
