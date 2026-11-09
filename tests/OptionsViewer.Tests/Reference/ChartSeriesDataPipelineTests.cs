using OptionsViewer.Core;
using OptionsViewer.Infrastructure;

namespace OptionsViewer.Tests;

/// <summary>
/// ChartSeriesData con la cadena completa (CSV → sesión → serie → arreglos): los huecos reales de los CSV de
/// referencia (CA-05) y el caso Ask(T0) = 0 con un CSV sintético.
/// </summary>
public class ChartSeriesDataPipelineTests
{
    [Fact]
    [Trait("RF", "RF-06")]
    [Trait("CA", "CA-05")]
    public void From_Calls337_5_T0_094449_NueveNaNEnLasPosicionesDeLosHuecos()
    {
        var t0 = new TimeOnly(9, 44, 49);
        var result = ValuationSeries.Build(ReferenceData.CallsSession, new ContractKey(OptionType.Call, 337.5m), t0);
        Assert.True(result.IsSuccess);
        var series = result.Value!;

        var data = ChartSeriesData.From(series);

        // Mismo largo en todos los arreglos: uno por punto de la serie
        Assert.Equal(series.Points.Count, data.Ask.Length);
        Assert.Equal(series.Points.Count, data.X.Length);

        // Ventana del hueco de CA-05: posiciones 0 (09:44:49, datos), 1..9 (huecos), 10 (09:45:29, datos)
        Assert.Equal(new TimeOnly(9, 44, 49), series.Points[0].Time);
        Assert.Equal(new TimeOnly(9, 45, 29), series.Points[10].Time);
        var window = Enumerable.Range(0, 11).ToList();
        var nanInWindow = window.Where(i => double.IsNaN(data.Ask[i])).ToList();
        Assert.Equal(9, nanInWindow.Count);                              // exactamente 9 NaN...
        Assert.Equal(Enumerable.Range(1, 9), nanInWindow);               // ...en las posiciones 1 a 9 (los huecos)
        Assert.False(double.IsNaN(data.Ask[0]));
        Assert.False(double.IsNaN(data.Ask[10]));

        // Las cuatro series coinciden en dónde están los NaN, y coinciden con los puntos Value == null de toda la serie
        // (el 337,5 vuelve a salir de la ventana de strikes más tarde, así que fuera de esas 11 posiciones hay más huecos reales)
        for (int i = 0; i < series.Points.Count; i++)
        {
            bool isGap = series.Points[i].Value is null;
            Assert.Equal(isGap, double.IsNaN(data.Ask[i]));
            Assert.Equal(isGap, double.IsNaN(data.Bid[i]));
            Assert.Equal(isGap, double.IsNaN(data.PnlDollars[i]));
            Assert.Equal(isGap, double.IsNaN(data.PnlPercent[i]));
            Assert.False(double.IsNaN(data.X[i]));
        }
    }

    [Fact]
    [Trait("RF", "RF-05")]
    public void From_Calls340_PrimerPunto_ValoresPorContratoDelEjemploDeCa04()
    {
        var result = ValuationSeries.Build(ReferenceData.CallsSession, new ContractKey(OptionType.Call, 340.0m), new TimeOnly(9, 30, 0));
        var series = result.Value!;

        var data = ChartSeriesData.From(series);

        Assert.Equal(34_200d, data.X[0]);
        Assert.Equal(300d, data.Ask[0]);
        Assert.Equal(289d, data.Bid[0]);
        Assert.Equal(-11d, data.PnlDollars[0]);
        Assert.Equal((double)(-11m / 300m * 100m), data.PnlPercent[0]);   // -3,6666..., sin redondear
        Assert.NotEqual(-3.67, data.PnlPercent[0]);
    }

    [Fact]
    [Trait("RF", "RF-05")]
    public void From_AskT0Cero_PnlPercentTodoNaN_YPnlDolaresNo()
    {
        // CSV sintético con Ask(T0) = 0
        string[] Row(string time, string bid, string ask) =>
            [time, "341.53", "340.0", bid, ask, "1.53", "1.47", "0.625", "0.193", "-0.325", "0.028", "42.582"];
        var csv = new SyntheticCsv()
            .AddRow(Row("09:30:00", "0.0", "0.0"))
            .AddRow(Row("09:30:04", "0.05", "0.10"))
            .AddRow(Row("09:30:08", "0.0", "0.05"))
            .Build();
        using var file = TempFile.WithContent(csv);
        var loaded = new CsvFileSnapshotSource(file.Path, OptionType.Call).Load(TestContext.Current.CancellationToken);
        Assert.True(loaded.IsSuccess, loaded.Error?.Message);
        var session = MarketHours.Apply(loaded.Value!);

        var series = ValuationSeries.Build(session, new ContractKey(OptionType.Call, 340.0m), new TimeOnly(9, 30, 0)).Value!;
        Assert.False(series.PercentAvailable);

        var data = ChartSeriesData.From(series);

        Assert.Equal(3, data.PnlPercent.Length);
        Assert.All(data.PnlPercent, v => Assert.True(double.IsNaN(v)));      // todo el PnL % es NaN...
        Assert.Equal([0d, 5d, 0d], data.PnlDollars);                          // ...y el PnL en $ no lo es
        Assert.All(data.PnlDollars, v => Assert.False(double.IsNaN(v)));
        Assert.Equal([0d, 10d, 5d], data.Ask);
    }
}
