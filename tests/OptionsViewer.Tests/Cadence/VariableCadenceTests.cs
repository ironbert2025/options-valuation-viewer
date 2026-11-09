using OptionsViewer.Core;
using OptionsViewer.Infrastructure;

namespace OptionsViewer.Tests;

/// <summary>
/// CA-13 / RNF-10: un CSV sintético con cadencia variable (p. ej. un snapshot cada 6 s hasta las 11:00 y uno por minuto
/// después, con snapshots también antes de 09:30:00 y después de 16:00:00) se procesa igual que los de referencia:
/// mismo filtro de horario, sin cortes falsos en las líneas y con el eje en tiempo real.
/// Los conteos esperados se calculan aquí a partir de las horas generadas, nunca escritos a mano.
/// </summary>
public class VariableCadenceTests
{
    private static readonly TimeOnly Open = new(9, 30, 0);
    private static readonly TimeOnly Close = new(16, 0, 0);
    private static readonly TimeOnly Eleven = new(11, 0, 0);

    private static readonly ContractKey AlwaysPresent = new(OptionType.Call, 340.0m);
    private static readonly ContractKey SometimesAbsent = new(OptionType.Call, 342.5m);

    private sealed record Scenario(
        List<TimeOnly> Times,                // todas las horas generadas, también fuera de horario
        HashSet<TimeOnly> WithoutSecond,     // horas donde falta el strike 342,5
        SnapshotSet Loaded,
        TradingSession Session);

    private static Scenario Make(int stepBefore, int stepAfter)
    {
        var preMarket = CadenceSegment.Of("08:30:00", "09:29:59", stepBefore);
        var regime1 = CadenceSegment.Of("09:30:00", "10:59:59", stepBefore);   // p. ej. 6 s → última 10:59:54
        var regime2 = CadenceSegment.Of("11:00:00", "16:00:00", stepAfter);    // p. ej. 1 min → última 16:00:00
        var postClose = CadenceSegment.Of("16:01:00", "17:00:00", stepAfter);

        var times = CadenceSegment.AllTimes([preMarket, regime1, regime2, postClose]);

        // Huecos definidos para el segundo contrato: 2 snapshots seguidos en cada régimen
        var r1 = regime1.Times().ToList();
        var r2 = regime2.Times().ToList();
        var withoutSecond = new HashSet<TimeOnly> { r1[10], r1[11], r2[5], r2[6] };

        using var file = TempFile.WithContent(SyntheticCsv.FromTimes(times, withoutSecond).Build());
        var result = new CsvFileSnapshotSource(file.Path, OptionType.Call).Load(TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess, result.Error?.Message);

        return new Scenario(times, withoutSecond, result.Value!, MarketHours.Apply(result.Value!));
    }

    private static List<TimeOnly> InSession(IEnumerable<TimeOnly> times) =>
        times.Where(t => t >= Open && t <= Close).ToList();

    private static ContractSeries BuildOk(Scenario s, ContractKey key, TimeOnly t0)
    {
        var result = ValuationSeries.Build(s.Session, key, t0);
        Assert.True(result.IsSuccess, result.Error is null ? null : $"{result.Error.Kind}");
        return result.Value!;
    }

    // ----- comprobaciones compartidas (cada prueba usa las que le corresponden)

    private static void AssertSameFilterAsReference(Scenario s)
    {
        var expectedInSession = InSession(s.Times);
        var expectedDiscarded = s.Times.Where(t => t < Open || t > Close).ToList();

        Assert.Equal(s.Times, s.Loaded.Snapshots.Select(x => x.Time));
        Assert.Equal(expectedInSession, s.Session.Snapshots.Select(x => x.Time));
        Assert.Equal(expectedDiscarded.Count, s.Session.DiscardedCount);
        Assert.False(s.Session.NoSnapshotsInWindow);
        Assert.Equal([340.0m, 342.5m], StrikeCatalog.List(s.Session));

        // Los descartados son exactamente los de antes de la apertura y después del cierre
        var keptTimes = s.Session.Snapshots.Select(x => x.Time).ToHashSet();
        Assert.Equal(expectedDiscarded, s.Loaded.Snapshots.Select(x => x.Time).Where(t => !keptTimes.Contains(t)));
    }

    private static void AssertNoFalseCuts(Scenario s)
    {
        var expected = InSession(s.Times);

        // El contrato presente en todos los snapshots: ningún hueco, cualquiera que sea la cadencia
        var always = BuildOk(s, AlwaysPresent, expected[0]);
        Assert.Equal(expected, always.Points.Select(p => p.Time));
        Assert.All(always.Points, p => Assert.NotNull(p.Value));

        // El que tiene huecos definidos: Value == null exactamente en esos snapshots y en ningún otro
        var sometimes = BuildOk(s, SometimesAbsent, expected[0]);
        Assert.Equal(expected, sometimes.Points.Select(p => p.Time));
        Assert.Equal(
            s.WithoutSecond.Order(),
            sometimes.Points.Where(p => p.Value is null).Select(p => p.Time));
    }

    private static void AssertRealTimeAxis(Scenario s)
    {
        var expected = InSession(s.Times);
        var series = BuildOk(s, AlwaysPresent, expected[0]);

        // SecondsOfDay de cada punto es la hora real del snapshot
        for (int i = 0; i < series.Points.Count; i++)
            Assert.Equal((int)expected[i].ToTimeSpan().TotalSeconds, series.Points[i].SecondsOfDay);

        // Y el eje es de tiempo real: la distancia entre puntos consecutivos es la diferencia real de horas,
        // no 1 (número de orden) ni una constante
        for (int i = 1; i < series.Points.Count; i++)
            Assert.Equal((expected[i] - expected[i - 1]).TotalSeconds, series.Points[i].SecondsOfDay - series.Points[i - 1].SecondsOfDay);

        Assert.Equal(34_200, series.Points[0].SecondsOfDay);                       // 09:30:00
        Assert.True(series.Points[^1].SecondsOfDay <= 57_600);                     // como mucho 16:00:00
    }

    // ----- pruebas

    [Fact]
    [Trait("RF", "RF-02")]
    [Trait("CA", "CA-13")]
    [Trait("RNF", "RNF-10")]
    public void Cadencia6sY1min_CargaIgualQueLaReferencia()
    {
        var s = Make(6, 60);

        AssertSameFilterAsReference(s);
        Assert.Equal(Open, s.Session.Snapshots[0].Time);
        Assert.Equal(Close, s.Session.Snapshots[^1].Time);
    }

    [Fact]
    [Trait("RF", "RF-06")]
    [Trait("CA", "CA-13")]
    [Trait("RNF", "RNF-10")]
    public void Cadencia6sY1min_SinCortesFalsosEnLaLinea()
    {
        AssertNoFalseCuts(Make(6, 60));
    }

    [Fact]
    [Trait("RF", "RF-06")]
    [Trait("CA", "CA-13")]
    [Trait("RNF", "RNF-10")]
    public void Cadencia6sY1min_SecondsOfDayCoincideConLaHoraReal()
    {
        AssertRealTimeAxis(Make(6, 60));
    }

    [Fact]
    [Trait("RF", "RF-06")]
    [Trait("CA", "CA-13")]
    [Trait("RNF", "RNF-10")]
    public void Cadencia6sY1min_ContratoPresenteEnTodos_NoGeneraHuecoAunqueCambieLaCadenciaALas11()
    {
        var s = Make(6, 60);

        var series = BuildOk(s, AlwaysPresent, Open);
        int at11 = series.Points.Select((p, i) => (p, i)).First(x => x.p.Time == Eleven).i;
        Assert.True(at11 > 0);

        // Antes de las 11:00 la cadencia es de 6 s y desde las 11:00 de 60 s; ningún punto de ese tramo es un hueco
        Assert.Equal(6, (series.Points[at11].Time - series.Points[at11 - 1].Time).TotalSeconds);   // 10:59:54 → 11:00:00
        Assert.Equal(60, (series.Points[at11 + 1].Time - series.Points[at11].Time).TotalSeconds);  // 11:00:00 → 11:01:00
        Assert.NotNull(series.Points[at11 - 1].Value);
        Assert.NotNull(series.Points[at11].Value);
        Assert.NotNull(series.Points[at11 + 1].Value);
        Assert.All(series.Points, p => Assert.NotNull(p.Value));
    }

    [Theory]
    [InlineData(4, 60)]
    [InlineData(5, 60)]
    [InlineData(15, 60)]
    [InlineData(6, 30)]
    [InlineData(6, 181)]
    [Trait("RF", "RF-02")]
    [Trait("RF", "RF-06")]
    [Trait("CA", "CA-13")]
    [Trait("RNF", "RNF-10")]
    public void OtrasCadencias_MismoComportamiento(int stepBefore, int stepAfter)
    {
        var s = Make(stepBefore, stepAfter);

        AssertSameFilterAsReference(s);
        AssertNoFalseCuts(s);
        AssertRealTimeAxis(s);
    }
}
