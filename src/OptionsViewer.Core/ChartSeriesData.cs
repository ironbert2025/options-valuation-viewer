namespace OptionsViewer.Core;

/// <summary>
/// La serie de un contrato convertida en arreglos de <c>double</c> listos para graficar, un elemento por punto de la serie.
/// Es una conversión de presentación en el borde del núcleo: los cálculos siguen en <c>decimal</c> y aquí solo se
/// convierte, sin redondear. Los huecos (contrato ausente de un snapshot) son <see cref="double.NaN"/> en las cuatro
/// series de valores, lo que corta la línea sin interpolar (RF-06); la X nunca es NaN.
/// </summary>
/// <param name="X">Eje horizontal en tiempo real: <see cref="SeriesPoint.SecondsOfDay"/> de cada punto.</param>
/// <param name="Ask">Ask por contrato (cotizado × 100).</param>
/// <param name="Bid">Bid por contrato (cotizado × 100).</param>
/// <param name="PnlDollars">Ganancia o pérdida en $ por contrato.</param>
/// <param name="PnlPercent">Ganancia o pérdida en %; todo NaN si el % no está disponible (Ask(T0) = 0, n/d).</param>
public record ChartSeriesData(double[] X, double[] Ask, double[] Bid, double[] PnlDollars, double[] PnlPercent)
{
    public static ChartSeriesData From(ContractSeries series)
    {
        int n = series.Points.Count;
        var x = new double[n];
        var ask = new double[n];
        var bid = new double[n];
        var pnlDollars = new double[n];
        var pnlPercent = new double[n];

        for (int i = 0; i < n; i++)
        {
            var point = series.Points[i];
            x[i] = point.SecondsOfDay;

            if (point.Value is { } value)
            {
                ask[i] = (double)value.AskPerContract;
                bid[i] = (double)value.BidPerContract;
                pnlDollars[i] = (double)value.PnlDollars;
                pnlPercent[i] = series.PercentAvailable && value.PnlPercent is { } pct ? (double)pct : double.NaN;
            }
            else
            {
                ask[i] = bid[i] = pnlDollars[i] = pnlPercent[i] = double.NaN;   // hueco
            }
        }

        return new ChartSeriesData(x, ask, bid, pnlDollars, pnlPercent);
    }
}
