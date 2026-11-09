using OptionsViewer.Core;

namespace OptionsViewer.Tests;

/// <summary>
/// Integración Infrastructure + Core con los CSV reales de samples/ (04-nov-2026):
/// el archivo cargado con <see cref="OptionsViewer.Infrastructure.CsvFileSnapshotSource"/> y filtrado con
/// <see cref="MarketHours.Apply"/> (cargados una sola vez, ver <see cref="ReferenceData"/>).
/// </summary>
public class ReferenceSessionTests
{
    private static readonly TimeOnly[] ExpectedDiscarded = [new(8, 24, 27), new(18, 24, 35)];

    private static void AssertCa01(SnapshotSet loaded, TradingSession session)
    {
        // Antes del filtro: 1.615 snapshots; después: 1.613 dentro y 2 descartados
        Assert.Equal(1615, loaded.Snapshots.Count);
        Assert.Equal(1613, session.Snapshots.Count);
        Assert.Equal(2, session.DiscardedCount);
        Assert.False(session.NoSnapshotsInWindow);

        // Los descartados son exactamente 08:24:27 y 18:24:35: se comparan las horas, no solo el conteo
        var keptTimes = session.Snapshots.Select(s => s.Time).ToHashSet();
        var discardedTimes = loaded.Snapshots.Select(s => s.Time).Where(t => !keptTimes.Contains(t)).ToList();
        Assert.Equal(ExpectedDiscarded, discardedTimes);

        // Y los 1.613 que quedan son todos de la sesión, de 09:30:00 a 15:59:06 (spec, día de referencia)
        Assert.All(session.Snapshots, s => Assert.True(MarketHours.IsInSession(s.Time), $"{s.Time} está fuera de horario"));
        Assert.Equal(new TimeOnly(9, 30, 0), session.Snapshots[0].Time);
        Assert.Equal(new TimeOnly(15, 59, 6), session.Snapshots[^1].Time);
    }

    [Fact]
    [Trait("RF", "RF-02")]
    [Trait("CA", "CA-01")]
    [Trait("RNF", "RNF-08")]
    public void Referencia_Calls_1613DentroYDescarta2()
    {
        Assert.Equal(OptionType.Call, ReferenceData.CallsSession.Type);
        AssertCa01(ReferenceData.CallsSet, ReferenceData.CallsSession);
    }

    [Fact]
    [Trait("RF", "RF-02")]
    [Trait("CA", "CA-01")]
    [Trait("RNF", "RNF-08")]
    public void Referencia_Puts_1613DentroYDescarta2()
    {
        Assert.Equal(OptionType.Put, ReferenceData.PutsSession.Type);
        AssertCa01(ReferenceData.PutsSet, ReferenceData.PutsSession);
    }
}
