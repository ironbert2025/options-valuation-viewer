using OptionsViewer.Core;

namespace OptionsViewer.Tests;

public class SeriesLookupTests
{
    private static readonly double[] Xs = [100, 200, 300, 400];

    [Fact]
    [Trait("RF", "RF-07")]
    public void FindNearestIndex_AntesDelPrimero_DevuelveCero()
    {
        Assert.Equal(0, SeriesLookup.FindNearestIndex(Xs, 50));
        Assert.Equal(0, SeriesLookup.FindNearestIndex(Xs, -1e9));
        Assert.Equal(0, SeriesLookup.FindNearestIndex(Xs, 99.999));
    }

    [Fact]
    [Trait("RF", "RF-07")]
    public void FindNearestIndex_DespuesDelUltimo_DevuelveUltimo()
    {
        Assert.Equal(3, SeriesLookup.FindNearestIndex(Xs, 401));
        Assert.Equal(3, SeriesLookup.FindNearestIndex(Xs, 1e9));
        Assert.Equal(3, SeriesLookup.FindNearestIndex(Xs, 400.001));
    }

    [Theory]
    [InlineData(120, 0)]
    [InlineData(149.999, 0)]
    [InlineData(150.001, 1)]
    [InlineData(180, 1)]
    [InlineData(260, 2)]
    [InlineData(349, 2)]
    [InlineData(351, 3)]
    [Trait("RF", "RF-07")]
    public void FindNearestIndex_EntreDosPuntos_ElMasCercano(double x, int expected)
    {
        Assert.Equal(expected, SeriesLookup.FindNearestIndex(Xs, x));
    }

    [Theory]
    [InlineData(150, 0)]    // equidista de 100 y 200 → el anterior
    [InlineData(250, 1)]    // equidista de 200 y 300 → el anterior
    [InlineData(350, 2)]    // equidista de 300 y 400 → el anterior
    [Trait("RF", "RF-07")]
    public void FindNearestIndex_EmpateExacto_ElAnterior(double x, int expected)
    {
        Assert.Equal(expected, SeriesLookup.FindNearestIndex(Xs, x));
    }

    [Theory]
    [InlineData(100, 0)]
    [InlineData(200, 1)]
    [InlineData(400, 3)]
    [Trait("RF", "RF-07")]
    public void FindNearestIndex_CoincidenciaExacta_DevuelveEsePunto(double x, int expected)
    {
        Assert.Equal(expected, SeriesLookup.FindNearestIndex(Xs, x));
    }

    [Fact]
    [Trait("RF", "RF-07")]
    public void FindNearestIndex_ArregloVacio_DevuelveMenosUno()
    {
        Assert.Equal(-1, SeriesLookup.FindNearestIndex([], 123));
    }

    [Fact]
    [Trait("RF", "RF-07")]
    public void FindNearestIndex_UnSoloElemento_DevuelveCero()
    {
        Assert.Equal(0, SeriesLookup.FindNearestIndex([500], 0));
        Assert.Equal(0, SeriesLookup.FindNearestIndex([500], 500));
        Assert.Equal(0, SeriesLookup.FindNearestIndex([500], 1e6));
    }

    [Fact]
    [Trait("RF", "RF-07")]
    public void FindNearestIndex_XNaN_DevuelveMenosUno()
    {
        Assert.Equal(-1, SeriesLookup.FindNearestIndex(Xs, double.NaN));
    }

    [Fact]
    [Trait("RF", "RF-07")]
    [Trait("RF", "RF-06")]
    public void FindNearestIndex_PuntoEnHueco_DevuelveElSnapshotDelHueco()
    {
        // La X incluye también los snapshots donde el contrato falta (el hueco): el más cercano en X es el del hueco,
        // aunque no tenga datos; la interfaz muestra entonces "sin dato" (D8)
        var series = new ContractSeries(
            new ContractKey(OptionType.Call, 340.0m), new TimeOnly(9, 30, 0), 300m, true,
            [
                new SeriesPoint(new TimeOnly(9, 30, 0), new PricePoint(300m, 289m, -11m, -3.67m)),
                new SeriesPoint(new TimeOnly(9, 30, 4), null),                                             // hueco
                new SeriesPoint(new TimeOnly(9, 30, 8), new PricePoint(320m, 305m, 5m, 1.67m)),
            ]);
        var data = ChartSeriesData.From(series);

        int index = SeriesLookup.FindNearestIndex(data.X, 34_203.2);   // 09:30:03,2: lo más cerca es el snapshot de 09:30:04

        Assert.Equal(1, index);
        Assert.Null(series.Points[index].Value);
        Assert.True(double.IsNaN(data.Ask[index]));
    }

    [Fact]
    [Trait("RF", "RF-07")]
    public void FindNearestIndex_ArregloGrandeDe23400_CoincideConBusquedaLinealDeReferencia()
    {
        // 23.400 elementos (un día de 6,5 h), estrictamente crecientes y con pasos irregulares de 1 a 3 s. Semilla fija.
        var rng = new Random(20261105);
        var xs = new double[23_400];
        double current = 34_200;
        for (int i = 0; i < xs.Length; i++)
        {
            xs[i] = current;
            current += rng.Next(1, 4);
        }

        // Muestras con la misma semilla (la referencia lineal es O(n) por consulta: se muestrea para no alargar la suite)
        const int RandomQueries = 3_000, ExactQueries = 2_000, TieQueries = 2_000;
        var queries = new List<double>();
        for (int i = 0; i < RandomQueries; i++)
            queries.Add(xs[0] - 50 + rng.NextDouble() * (xs[^1] - xs[0] + 100));      // aleatorios, dentro y fuera de rango
        for (int i = 0; i < ExactQueries; i++)
            queries.Add(xs[rng.Next(xs.Length)]);                                       // coincidencias exactas
        for (int i = 0; i < TieQueries; i++)
        {
            int k = rng.Next(xs.Length - 1);
            queries.Add((xs[k] + xs[k + 1]) / 2);                                       // empates exactos (a medio camino)
        }
        queries.AddRange([xs[0] - 1, xs[0], xs[^1], xs[^1] + 1]);                       // extremos

        int ties = 0;
        foreach (var x in queries)
        {
            int expected = LinearNearest(xs, x);
            Assert.Equal(expected, SeriesLookup.FindNearestIndex(xs, x));
            if (expected + 1 < xs.Length && x - xs[expected] == xs[expected + 1] - x) ties++;
        }

        Assert.True(ties >= TieQueries, $"La prueba debe incluir al menos {TieQueries} empates exactos (incluidos: {ties}).");
    }

    /// <summary>Referencia lineal: el índice del punto más cercano; en un empate, el primero (el anterior).</summary>
    private static int LinearNearest(double[] xs, double x)
    {
        int best = 0;
        double bestDistance = Math.Abs(xs[0] - x);
        for (int i = 1; i < xs.Length; i++)
        {
            double distance = Math.Abs(xs[i] - x);
            if (distance < bestDistance)   // estrictamente menor: en un empate se conserva el anterior
            {
                best = i;
                bestDistance = distance;
            }
        }
        return best;
    }
}
