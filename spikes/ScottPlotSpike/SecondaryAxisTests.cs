using System.Drawing;
using ScottPlot;

namespace ScottPlotSpike;

public class SecondaryAxisTests
{
    private const int Width = 700;
    private const int Height = 400;

    [Fact]
    public void Spike_EjeSecundario_EscalaPropia()
    {
        var plot = new Plot();

        // Serie 1 (eje izquierdo, "PnL $"): constante 15, límites -30..30
        double[] xs = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        var left = plot.Add.Scatter(xs, Enumerable.Repeat(15.0, xs.Length).ToArray());
        left.Color = Colors.Red;
        left.LineWidth = 4;
        left.MarkerSize = 0;

        // Serie 2 (eje derecho, "PnL %"): constante 2,5, límites -10..10
        var right = plot.Axes.AddRightAxis();
        var pct = plot.Add.Scatter(xs, Enumerable.Repeat(2.5, xs.Length).ToArray());
        pct.Color = Colors.Blue;
        pct.LineWidth = 4;
        pct.MarkerSize = 0;
        pct.Axes.YAxis = right;

        plot.Axes.Left.Label.Text = "PnL $";
        right.LabelText = "PnL %";
        plot.Axes.SetLimitsX(0, 10);
        plot.Axes.SetLimitsY(-30, 30, plot.Axes.Left);
        plot.Axes.SetLimitsY(-10, 10, right);

        byte[] png = plot.GetImageBytes(Width, Height, ScottPlot.ImageFormat.Png);
        using var bmp = new Bitmap(new MemoryStream(png));

        // Fila de píxel esperada para cada serie según su PROPIO eje
        float rowRed = plot.GetPixel(new Coordinates(5, 15), plot.Axes.Bottom, plot.Axes.Left).Y;
        float rowBlue = plot.GetPixel(new Coordinates(5, 2.5), plot.Axes.Bottom, right).Y;
        // Dónde estaría la serie azul si se hubiera dibujado con la escala del eje izquierdo
        float rowBlueIfLeftScale = plot.GetPixel(new Coordinates(5, 2.5), plot.Axes.Bottom, plot.Axes.Left).Y;

        int x = (int)plot.GetPixel(new Coordinates(5, 15), plot.Axes.Bottom, plot.Axes.Left).X;
        bool redAtRowRed = HasColorNear(bmp, x, (int)Math.Round(rowRed), IsRed);
        bool blueAtRowBlue = HasColorNear(bmp, x, (int)Math.Round(rowBlue), IsBlue);
        bool blueAtWrongRow = HasColorNear(bmp, x, (int)Math.Round(rowBlueIfLeftScale), IsBlue);

        Log($"[spike eje2] filas esperadas: rojo(eje izq, 15$) = {rowRed:F1}; azul(eje der, 2,5%) = {rowBlue:F1}; azul si usara escala izq = {rowBlueIfLeftScale:F1}");
        Log($"[spike eje2] rojo en su fila: {redAtRowRed}; azul en su fila (escala derecha): {blueAtRowBlue}; azul en la fila de escala izquierda: {blueAtWrongRow}");
        Log($"[spike eje2] ejes Y del plot: izquierdo={plot.Axes.Left.GetType().Name}, derecho={right.GetType().Name}; límites der = {right.Min}..{right.Max}; límites izq = {plot.Axes.Left.Min}..{plot.Axes.Left.Max}");

        Assert.True(Math.Abs(rowBlue - rowBlueIfLeftScale) > 10, "Las escalas no son distintas: el control no discrimina.");
        Assert.True(redAtRowRed, "La serie del eje izquierdo no está donde marca su escala.");
        Assert.True(blueAtRowBlue, "La serie del eje derecho no está donde marca su escala.");
        Assert.False(blueAtWrongRow, "La serie del eje derecho se dibujó con la escala del eje izquierdo.");
    }

    private static bool IsRed(System.Drawing.Color c) => c.R > 200 && c.G < 80 && c.B < 80;
    private static bool IsBlue(System.Drawing.Color c) => c.B > 200 && c.R < 80 && c.G < 80;

    private static bool HasColorNear(Bitmap bmp, int x, int row, Func<System.Drawing.Color, bool> match)
    {
        for (int y = row - 3; y <= row + 3; y++)
            if (y >= 0 && y < bmp.Height && match(bmp.GetPixel(x, y))) return true;
        return false;
    }

    private static void Log(string line) =>
        File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "spike-output.txt"), line + Environment.NewLine);
}
