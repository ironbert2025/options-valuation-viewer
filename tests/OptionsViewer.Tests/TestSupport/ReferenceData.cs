using OptionsViewer.Core;
using OptionsViewer.Infrastructure;

namespace OptionsViewer.Tests;

/// <summary>
/// Los CSV de referencia de samples/ cargados una sola vez y compartidos entre todas las pruebas.
/// Los resultados son inmutables (records y listas de solo lectura que ninguna prueba modifica), por lo que
/// compartirlos es seguro incluso con las pruebas ejecutándose en paralelo. Carga perezosa y segura entre hilos.
/// Las pruebas que verifican la propia lectura del archivo (CsvFileSnapshotSourceTests) siguen cargando por su cuenta.
/// </summary>
public static class ReferenceData
{
    private static readonly Lazy<SnapshotSet> CallsLoaded = new(() => LoadOrThrow(SamplePaths.Calls, OptionType.Call));
    private static readonly Lazy<SnapshotSet> PutsLoaded = new(() => LoadOrThrow(SamplePaths.Puts, OptionType.Put));
    private static readonly Lazy<TradingSession> CallsFiltered = new(() => MarketHours.Apply(CallsLoaded.Value));
    private static readonly Lazy<TradingSession> PutsFiltered = new(() => MarketHours.Apply(PutsLoaded.Value));

    /// <summary>Calls completo, sin filtrar (1.615 snapshots).</summary>
    public static SnapshotSet CallsSet => CallsLoaded.Value;

    /// <summary>Puts completo, sin filtrar (1.615 snapshots).</summary>
    public static SnapshotSet PutsSet => PutsLoaded.Value;

    /// <summary>Calls dentro de 09:30:00 a 16:00:00 (1.613 snapshots).</summary>
    public static TradingSession CallsSession => CallsFiltered.Value;

    /// <summary>Puts dentro de 09:30:00 a 16:00:00 (1.613 snapshots).</summary>
    public static TradingSession PutsSession => PutsFiltered.Value;

    private static SnapshotSet LoadOrThrow(string path, OptionType type)
    {
        var result = new CsvFileSnapshotSource(path, type).Load(CancellationToken.None);
        return result.IsSuccess
            ? result.Value!
            : throw new InvalidOperationException($"No se pudo cargar {path}: {result.Error!.Message}");
    }
}
