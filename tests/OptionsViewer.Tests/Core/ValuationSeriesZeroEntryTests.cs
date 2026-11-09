using OptionsViewer.Core;
using OptionsViewer.Infrastructure;

namespace OptionsViewer.Tests;

/// <summary>
/// Ask(T0) = 0: el % no se puede calcular y se muestra como n/d; el PnL en $ sí se calcula.
/// En los CSV de referencia el Ask nunca vale 0 (mínimo 0,02), así que se usa un CSV sintético.
/// </summary>
public class ValuationSeriesZeroEntryTests
{
    private static readonly ContractKey Call340 = new(OptionType.Call, 340.0m);
    private static readonly TimeOnly T0 = new(9, 30, 0);

    // Time, Spot, Strike, Bid, Ask, Int, Ext, Delta, Gamma, Theta, Vega, IV
    private static string[] Row(string time, string bid, string ask) =>
        [time, "341.53", "340.0", bid, ask, "1.53", "1.47", "0.625", "0.193", "-0.325", "0.028", "42.582"];

    private static TradingSession SyntheticSession()
    {
        var csv = new SyntheticCsv()
            .AddRow(Row("09:30:00", "0.0", "0.0"))     // T0: Ask = 0
            .AddRow(Row("09:30:04", "0.05", "0.10"))   // Bid 0,05 → 5 $ por contrato
            .AddRow(Row("09:30:08", "0.0", "0.05"))
            .Build();
        using var file = TempFile.WithContent(csv);
        var result = new CsvFileSnapshotSource(file.Path, OptionType.Call).Load(TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess, result.Error?.Message);
        return MarketHours.Apply(result.Value!);
    }

    private static ContractSeries BuildOk()
    {
        var result = ValuationSeries.Build(SyntheticSession(), Call340, T0);
        Assert.True(result.IsSuccess, result.Error is null ? null : $"{result.Error.Kind}");
        return result.Value!;
    }

    [Fact]
    [Trait("RF", "RF-05")]
    public void Build_AskT0Cero_PercentAvailableFalse()
    {
        var series = BuildOk();

        Assert.Equal(0m, series.EntryAskPerContract);
        Assert.False(series.PercentAvailable);
    }

    [Fact]
    [Trait("RF", "RF-05")]
    public void Build_AskT0Cero_PnlPercentNulosEnTodosLosPuntos()
    {
        var series = BuildOk();

        Assert.Equal(3, series.Points.Count);
        Assert.All(series.Points, p => Assert.Null(p.Value!.PnlPercent));
    }

    [Fact]
    [Trait("RF", "RF-05")]
    public void Build_AskT0Cero_PnlDolaresSeCalcula()
    {
        var series = BuildOk();

        // PnL = Bid × 100 − Ask(T0) × 100, con Ask(T0) = 0
        Assert.Equal([0m, 5m, 0m], series.Points.Select(p => p.Value!.PnlDollars));
        Assert.Equal([0m, 10m, 5m], series.Points.Select(p => p.Value!.AskPerContract));
    }

    [Fact]
    [Trait("RF", "RF-05")]
    public void Build_AskT0Cero_NoLanzaDivisionEntreCero()
    {
        var session = SyntheticSession();

        var exception = Record.Exception(() => ValuationSeries.Build(session, Call340, T0));

        Assert.Null(exception);
    }

    [Fact]
    [Trait("RF", "RF-05")]
    public void Build_AskT0Positivo_PercentAvailableTrueYPorcentajesCalculados()
    {
        // Control: con Ask(T0) > 0 (Calls 340 a las 09:30:00 = 3,00) el % sí está disponible
        var result = ValuationSeries.Build(ReferenceData.CallsSession, Call340, T0);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.PercentAvailable);
        Assert.All(result.Value.Points.Where(p => p.Value is not null), p => Assert.NotNull(p.Value!.PnlPercent));
    }
}
