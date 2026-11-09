using OptionsViewer.Core;

namespace OptionsViewer.Tests;

public class ChartSeriesDataTests
{
    private static readonly ContractKey Call340 = new(OptionType.Call, 340.0m);

    private static PricePoint Price(decimal ask, decimal bid, decimal pnl, decimal? pct) => new(ask, bid, pnl, pct);

    private static ContractSeries SeriesOf(bool percentAvailable, params SeriesPoint[] points) =>
        new(Call340, new TimeOnly(9, 30, 0), 300m, percentAvailable, points);

    [Fact]
    [Trait("RF", "RF-06")]
    public void From_PuntoConHueco_YNaN()
    {
        var series = SeriesOf(true,
            new SeriesPoint(new TimeOnly(9, 30, 0), Price(300m, 289m, -11m, -3.67m)),
            new SeriesPoint(new TimeOnly(9, 30, 4), null),                                    // hueco
            new SeriesPoint(new TimeOnly(9, 30, 8), Price(320m, 305m, 5m, 1.67m)));

        var data = ChartSeriesData.From(series);

        // El punto con hueco es NaN en las cuatro series (ScottPlot corta la línea en un NaN)
        foreach (var array in new[] { data.Ask, data.Bid, data.PnlDollars, data.PnlPercent })
        {
            Assert.Equal(3, array.Length);
            Assert.False(double.IsNaN(array[0]));
            Assert.True(double.IsNaN(array[1]));
            Assert.False(double.IsNaN(array[2]));
        }
        Assert.Equal(3, data.X.Length);
        Assert.All(data.X, x => Assert.False(double.IsNaN(x)));   // la X nunca es NaN, tampoco en el hueco
    }

    [Fact]
    [Trait("RF", "RF-06")]
    public void From_X_EsSecondsOfDay()
    {
        var series = SeriesOf(true,
            new SeriesPoint(new TimeOnly(9, 30, 0), Price(300m, 289m, -11m, -3.67m)),
            new SeriesPoint(new TimeOnly(9, 30, 4), null),
            new SeriesPoint(new TimeOnly(11, 0, 0), Price(320m, 305m, 5m, 1.67m)),
            new SeriesPoint(new TimeOnly(16, 0, 0), Price(320m, 305m, 5m, 1.67m)));

        var data = ChartSeriesData.From(series);

        Assert.Equal([34_200d, 34_204d, 39_600d, 57_600d], data.X);   // tiempo real, no el número de orden
        Assert.Equal(series.Points.Select(p => (double)p.SecondsOfDay), data.X);
    }

    [Fact]
    [Trait("RF", "RF-05")]
    public void From_AskCero_PnlPercentTodoNaN()
    {
        // PercentAvailable = false: el % es n/d en toda la serie, aunque algún punto traiga un valor
        var series = SeriesOf(false,
            new SeriesPoint(new TimeOnly(9, 30, 0), Price(0m, 0m, 0m, null)),
            new SeriesPoint(new TimeOnly(9, 30, 4), Price(10m, 5m, 5m, null)),
            new SeriesPoint(new TimeOnly(9, 30, 8), Price(5m, 0m, 0m, 123.45m)));

        var data = ChartSeriesData.From(series);

        Assert.All(data.PnlPercent, v => Assert.True(double.IsNaN(v)));
        Assert.Equal([0d, 5d, 0d], data.PnlDollars);                        // el PnL en $ sí se dibuja
        Assert.All(data.PnlDollars, v => Assert.False(double.IsNaN(v)));
    }

    [Fact]
    [Trait("RF", "RF-05")]
    public void From_PnlPercentNulo_ConPercentAvailable_EsNaNSoloEnEsePunto()
    {
        var series = SeriesOf(true,
            new SeriesPoint(new TimeOnly(9, 30, 0), Price(300m, 289m, -11m, -3.67m)),
            new SeriesPoint(new TimeOnly(9, 30, 4), Price(320m, 305m, 5m, null)));

        var data = ChartSeriesData.From(series);

        Assert.False(double.IsNaN(data.PnlPercent[0]));
        Assert.True(double.IsNaN(data.PnlPercent[1]));
        Assert.False(double.IsNaN(data.PnlDollars[1]));
    }

    [Fact]
    [Trait("RF", "RF-05")]
    public void From_ValoresPorContratoSinRedondear()
    {
        // Los valores ya son por contrato (× 100) y la conversión a double no redondea
        var series = SeriesOf(true,
            new SeriesPoint(new TimeOnly(9, 30, 0), Price(300.123456m, 289.987654m, -10.135802m, -3.6666666666666666666666666700m)));

        var data = ChartSeriesData.From(series);

        Assert.Equal((double)300.123456m, data.Ask[0]);
        Assert.Equal((double)289.987654m, data.Bid[0]);
        Assert.Equal((double)-10.135802m, data.PnlDollars[0]);
        Assert.Equal((double)-3.6666666666666666666666666700m, data.PnlPercent[0]);
        Assert.NotEqual(-3.67, data.PnlPercent[0]);   // sin redondear a 2 decimales
    }

    [Fact]
    [Trait("RF", "RF-05")]
    public void From_SerieSinPuntos_ArreglosVacios()
    {
        var data = ChartSeriesData.From(SeriesOf(true));

        Assert.Empty(data.X);
        Assert.Empty(data.Ask);
        Assert.Empty(data.Bid);
        Assert.Empty(data.PnlDollars);
        Assert.Empty(data.PnlPercent);
    }
}
