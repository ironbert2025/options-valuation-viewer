using OptionsViewer.Core;

namespace OptionsViewer.Tests;

/// <summary>Valores de la serie (entrada, Bid, PnL en $ y en %) con los CSV reales de samples/. CA-04.</summary>
public class ReferenceSeriesValuesTests
{
    private static readonly TimeOnly Open = new(9, 30, 0);

    private static ContractSeries BuildOk(ContractKey key, TimeOnly t0)
    {
        var result = ValuationSeries.Build(ReferenceData.CallsSession, key, t0);
        Assert.True(result.IsSuccess, result.Error is null ? null : $"{result.Error.Kind}");
        return result.Value!;
    }

    [Fact]
    [Trait("RF", "RF-05")]
    [Trait("CA", "CA-04")]
    public void Build_Calls340T0_0930_EntradaBidYPnlDelEjemplo()
    {
        var key = new ContractKey(OptionType.Call, 340.0m);

        var series = BuildOk(key, Open);

        Assert.Equal(key, series.Key);
        Assert.Equal(Open, series.T0);
        Assert.Equal(300m, series.EntryAskPerContract);          // Ask 3,00 × 100

        var first = series.Points[0];
        Assert.Equal(Open, first.Time);                            // el primer punto es T0
        var v = first.Value!;
        Assert.Equal(300m, v.AskPerContract);
        Assert.Equal(289m, v.BidPerContract);                      // Bid 2,89 × 100
        Assert.Equal(-11m, v.PnlDollars);                          // 289 − 300

        // PnL% = (289 − 300) / 300 × 100. Sin redondear, con el orden del diseño (pnl / entrada × 100):
        //   -3.6666666666666666666666666700   (con ×100 primero saldría -3.6666666666666666666666666667)
        // y a 2 decimales -3,67 (CA-04).
        Assert.Equal(-11m / 300m * 100m, v.PnlPercent);
        Assert.Equal(-3.67m, Math.Round(v.PnlPercent!.Value, 2));
    }

    [Fact]
    [Trait("RF", "RF-05")]
    [Trait("CA", "CA-04")]
    public void Build_Calls340_PuntosPosteriores_UsanElAskDeT0ParaElPorcentaje()
    {
        // Datos reales de los primeros snapshots (Bid/Ask cotizados):
        //   09:30:04 3.05/3.35   09:30:08 3.05/3.2   09:30:12 3.35/3.75   09:30:16 3.35/3.5
        // Entrada = Ask(09:30:00) × 100 = 300. El % siempre es sobre ese 300, no sobre el Ask del propio punto.
        var series = BuildOk(new ContractKey(OptionType.Call, 340.0m), Open);

        (TimeOnly Time, decimal Ask, decimal Bid, decimal Pnl, decimal Pct)[] expected =
        [
            (new(9, 30, 4), 335m, 305m, 5m, 1.6667m),
            (new(9, 30, 8), 320m, 305m, 5m, 1.6667m),
            (new(9, 30, 12), 375m, 335m, 35m, 11.6667m),
            (new(9, 30, 16), 350m, 335m, 35m, 11.6667m),
        ];

        for (int i = 0; i < expected.Length; i++)
        {
            var point = series.Points[i + 1];
            var e = expected[i];
            Assert.Equal(e.Time, point.Time);
            Assert.Equal(e.Ask, point.Value!.AskPerContract);
            Assert.Equal(e.Bid, point.Value.BidPerContract);
            Assert.Equal(e.Pnl, point.Value.PnlDollars);
            Assert.Equal(e.Pct, Math.Round(point.Value.PnlPercent!.Value, 4));
        }
    }

    [Fact]
    [Trait("RF", "RF-05")]
    [Trait("CA", "CA-04")]
    public void Build_Calls342_5_PuntosDesdeT0HastaElUltimoSnapshot_ConLosValoresDeCadaSnapshot()
    {
        // El 342,5 está en todos los snapshots de la sesión (spec): un punto con datos por cada uno, desde T0 hasta el último
        var key = new ContractKey(OptionType.Call, 342.5m);
        var session = ReferenceData.CallsSession;

        var series = BuildOk(key, Open);

        var snapshots = session.Snapshots.Where(s => s.Time >= Open).ToList();
        Assert.Equal(snapshots.Count, series.Points.Count);
        Assert.Equal(new TimeOnly(15, 59, 6), series.Points[^1].Time);

        decimal entry = snapshots[0].QuotesByStrike[342.5m].Ask * 100m;
        Assert.Equal(entry, series.EntryAskPerContract);

        for (int i = 0; i < snapshots.Count; i++)
        {
            var q = snapshots[i].QuotesByStrike[342.5m];
            var point = series.Points[i];
            Assert.Equal(snapshots[i].Time, point.Time);
            Assert.NotNull(point.Value);
            Assert.Equal(q.Ask * 100m, point.Value.AskPerContract);
            Assert.Equal(q.Bid * 100m, point.Value.BidPerContract);
            Assert.Equal(q.Bid * 100m - entry, point.Value.PnlDollars);
            Assert.Equal((q.Bid * 100m - entry) / entry * 100m, point.Value.PnlPercent);
        }
    }

    [Fact]
    [Trait("RF", "RF-05")]
    public void Build_ValoresSonExactosEnDecimal()
    {
        // 2,89 × 100 en double da 289,00000000000006 y el PnL saldría -10,99999...; con decimal es exacto
        var series = BuildOk(new ContractKey(OptionType.Call, 340.0m), Open);

        Assert.Equal(289m, series.Points[0].Value!.BidPerContract);
        Assert.Equal(-11m, series.Points[0].Value!.PnlDollars);
    }
}
