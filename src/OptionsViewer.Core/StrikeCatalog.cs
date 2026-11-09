namespace OptionsViewer.Core;

public static class StrikeCatalog
{
    /// <summary>
    /// Strikes distintos que aparecen en los snapshots de la sesión (solo los de horario), ordenados de menor a mayor
    /// (RF-03). Lista vacía si la sesión no tiene snapshots.
    /// </summary>
    public static IReadOnlyList<decimal> List(TradingSession session) =>
        session.Snapshots
            .SelectMany(snapshot => snapshot.QuotesByStrike.Keys)
            .Distinct()
            .Order()
            .ToList();
}
