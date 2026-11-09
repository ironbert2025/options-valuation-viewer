using OptionsViewer.Core;
using OptionsViewer.Infrastructure;

namespace OptionsViewer.Tests;

public class SnapshotAccumulatorTests
{
    private static ParsedRow Row(string time, decimal strike) =>
        new(TimeOnly.ParseExact(time, "HH:mm:ss"),
            new ContractQuote(strike, 341.5m, 2.89m, 3.0m, 1.53m, 1.47m, 0.6m, 0.1m, -0.3m, 0.03m, 42.5m));

    private static IReadOnlyList<Snapshot> Build(params (string Time, decimal Strike)[] rows)
    {
        var acc = new SnapshotAccumulator();
        int line = 2;
        foreach (var (time, strike) in rows)
        {
            Assert.True(acc.TryAdd(Row(time, strike), line++, out var error), error?.Message);
        }
        return acc.Build();
    }

    [Fact]
    [Trait("RF", "RF-02")]
    public void Group_FilasConMismaTime_UnSoloSnapshot()
    {
        var snapshots = Build(("09:30:00", 337.5m), ("09:30:00", 340.0m), ("09:30:00", 342.5m));

        var snapshot = Assert.Single(snapshots);
        Assert.Equal(new TimeOnly(9, 30, 0), snapshot.Time);
        Assert.Equal([337.5m, 340.0m, 342.5m], snapshot.QuotesByStrike.Keys.Order());
    }

    [Fact]
    [Trait("RF", "RF-02")]
    public void Group_TimesDistintas_SnapshotsSeparados()
    {
        var snapshots = Build(
            ("09:30:00", 340.0m), ("09:30:00", 342.5m),
            ("09:30:04", 340.0m),
            ("09:30:08", 340.0m), ("09:30:08", 342.5m), ("09:30:08", 345.0m));

        Assert.Equal(3, snapshots.Count);
        Assert.Equal([new TimeOnly(9, 30, 0), new TimeOnly(9, 30, 4), new TimeOnly(9, 30, 8)], snapshots.Select(s => s.Time));
        Assert.Equal([2, 1, 3], snapshots.Select(s => s.QuotesByStrike.Count));
    }

    [Fact]
    [Trait("RF", "RF-02")]
    public void Group_SinFilas_ListaVacia()
    {
        Assert.Empty(new SnapshotAccumulator().Build());
    }

    [Fact]
    [Trait("RF", "RF-02")]
    [Trait("CA", "CA-02")]
    public void Group_TimeRetrocede_OutOfOrderConLinea()
    {
        var acc = new SnapshotAccumulator();
        Assert.True(acc.TryAdd(Row("09:30:04", 340.0m), 2, out _));
        Assert.True(acc.TryAdd(Row("09:30:08", 340.0m), 3, out _));

        Assert.False(acc.TryAdd(Row("09:30:04", 340.0m), 4, out var error));
        Assert.Equal(CsvErrorKind.OutOfOrder, error.Kind);
        Assert.Equal(4, error.Line);
        Assert.Equal("Time", error.Column);
        Assert.Equal("09:30:04", error.RawValue);
        Assert.Contains("4", error.Message);
    }

    [Fact]
    [Trait("RF", "RF-02")]
    [Trait("CA", "CA-02")]
    public void Group_StrikeRepetido_DuplicateStrikeConLinea()
    {
        var acc = new SnapshotAccumulator();
        Assert.True(acc.TryAdd(Row("09:30:00", 340.0m), 2, out _));
        Assert.True(acc.TryAdd(Row("09:30:00", 342.5m), 3, out _));

        Assert.False(acc.TryAdd(Row("09:30:00", 340.0m), 4, out var error));
        Assert.Equal(CsvErrorKind.DuplicateStrike, error.Kind);
        Assert.Equal(4, error.Line);
        Assert.Equal("StrikePrice", error.Column);
        Assert.Equal("340.0", error.RawValue);
    }

    [Fact]
    [Trait("RF", "RF-02")]
    public void Group_MismoStrikeEnSnapshotsDistintos_NoEsDuplicado()
    {
        var snapshots = Build(("09:30:00", 340.0m), ("09:30:04", 340.0m));

        Assert.Equal(2, snapshots.Count);
    }

    [Fact]
    [Trait("RF", "RF-02")]
    [Trait("RNF", "RNF-10")]
    public void Group_SaltosDe4_60_181Segundos_NoAlteraLaAgrupacion()
    {
        // Saltos de 4 s, 60 s y 181 s entre snapshots: la agrupación solo depende de que la Time cambie.
        var snapshots = Build(
            ("09:30:00", 340.0m), ("09:30:00", 342.5m),
            ("09:30:04", 340.0m), ("09:30:04", 342.5m),     // +4 s
            ("09:31:04", 340.0m), ("09:31:04", 342.5m),     // +60 s
            ("09:34:05", 340.0m), ("09:34:05", 342.5m));    // +181 s

        Assert.Equal(4, snapshots.Count);
        Assert.All(snapshots, s => Assert.Equal(2, s.QuotesByStrike.Count));
        Assert.Equal(
            [new TimeOnly(9, 30, 0), new TimeOnly(9, 30, 4), new TimeOnly(9, 31, 4), new TimeOnly(9, 34, 5)],
            snapshots.Select(s => s.Time));
    }

    [Fact]
    [Trait("RF", "RF-02")]
    public void Group_PrimerSnapshotConHoraFueraDeHorario_SeConserva()
    {
        // El agrupador no filtra por horario (eso lo hace MarketHours en la etapa 3)
        var snapshots = Build(("08:24:27", 337.5m), ("18:24:35", 340.0m));

        Assert.Equal(2, snapshots.Count);
    }
}
