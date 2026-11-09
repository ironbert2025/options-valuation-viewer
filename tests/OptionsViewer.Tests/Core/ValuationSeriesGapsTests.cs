using OptionsViewer.Core;

namespace OptionsViewer.Tests;

public class ValuationSeriesGapsTests
{
    private static readonly ContractKey Call340 = new(OptionType.Call, 340.0m);

    private static Snapshot Snap(string time, params decimal[] strikes) =>
        new(TimeOnly.ParseExact(time, "HH:mm:ss"),
            strikes.ToDictionary(
                s => s,
                s => new ContractQuote(s, 341.5m, 2.89m, 3.0m, 1.5m, 1.4m, 0.6m, 0.1m, -0.3m, 0.03m, 42.5m)));

    private static TradingSession SessionOf(params Snapshot[] snapshots) =>
        new(OptionType.Call, snapshots, DiscardedCount: 0, NoSnapshotsInWindow: false);

    [Theory]
    [InlineData("09:31:04", "09:34:05")]   // saltos de 60 s y de 181 s entre snapshots consecutivos
    [Trait("RF", "RF-06")]
    [Trait("CA", "CA-05")]
    [Trait("RNF", "RNF-10")]
    public void Build_ContratoEnSnapshotsConsecutivosSeparados60y181s_SinHueco(string after60, string after181)
    {
        var session = SessionOf(
            Snap("09:30:00", 340m), Snap("09:30:04", 340m),   // +4 s
            Snap(after60, 340m),                               // +60 s
            Snap(after181, 340m));                             // +181 s

        var result = ValuationSeries.Build(session, Call340, new TimeOnly(9, 30, 0));

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value!.Points.Count);
        Assert.All(result.Value.Points, p => Assert.NotNull(p.Value));
    }

    [Fact]
    [Trait("RF", "RF-06")]
    [Trait("CA", "CA-05")]
    [Trait("RNF", "RNF-10")]
    public void Build_UsaPosicionDeSnapshotNoDiferenciaDeTiempo_CadenciaCambiaYNoCreaHuecos()
    {
        // Cadencia de 6 s hasta las 11:00 y de 1 min después: ningún intervalo se interpreta como corte
        var session = SessionOf(
            Snap("10:59:48", 340m), Snap("10:59:54", 340m), Snap("11:00:00", 340m),
            Snap("11:01:00", 340m), Snap("11:02:00", 340m), Snap("11:05:01", 340m));

        var result = ValuationSeries.Build(session, Call340, new TimeOnly(10, 59, 48));

        Assert.Equal(6, result.Value!.Points.Count);
        Assert.All(result.Value.Points, p => Assert.NotNull(p.Value));
    }

    [Fact]
    [Trait("RF", "RF-06")]
    [Trait("CA", "CA-05")]
    public void Build_ContratoAusenteEnUnSnapshotIntermedio_EmiteUnPuntoConValueNull()
    {
        var session = SessionOf(
            Snap("09:30:00", 340m, 342.5m),
            Snap("09:30:04", 342.5m),                // el 340 falta aquí
            Snap("09:30:08", 340m, 342.5m));

        var result = ValuationSeries.Build(session, Call340, new TimeOnly(9, 30, 0));

        var points = result.Value!.Points;
        Assert.Equal(3, points.Count);
        Assert.NotNull(points[0].Value);
        Assert.Equal(new TimeOnly(9, 30, 4), points[1].Time);
        Assert.Null(points[1].Value);
        Assert.NotNull(points[2].Value);
    }

    [Fact]
    [Trait("RF", "RF-06")]
    [Trait("CA", "CA-05")]
    public void Build_ContratoAusenteVariosSnapshotsSeguidos_UnHuecoPorCadaSnapshotAusente()
    {
        var session = SessionOf(
            Snap("09:30:00", 340m), Snap("09:30:04", 342.5m), Snap("09:30:08", 342.5m), Snap("09:30:12", 342.5m), Snap("09:30:16", 340m));

        var points = ValuationSeries.Build(session, Call340, new TimeOnly(9, 30, 0)).Value!.Points;

        Assert.Equal([true, false, false, false, true], points.Select(p => p.Value is not null));
    }
}
