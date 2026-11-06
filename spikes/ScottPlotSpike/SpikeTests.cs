using System.Drawing;
using ScottPlot;
using ScottPlot.WinForms;

namespace ScottPlotSpike;

public class SpikeTests
{
    private const int Width = 600;
    private const int Height = 300;

    // MTP no muestra stdout; las mediciones se guardan en un archivo junto al ejecutable.
    private static readonly object LogLock = new();
    private static void Log(string line)
    {
        lock (LogLock) File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "spike-output.txt"), line + Environment.NewLine);
    }

    private static Plot BuildPlot(bool withNaN)
    {
        // Línea horizontal y = 5 con un NaN en x = 5 (vecinos presentes en x = 4 y x = 6)
        double[] xs = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        double[] ys = [5, 5, 5, 5, 5, withNaN ? double.NaN : 5, 5, 5, 5, 5, 5];

        var plot = new Plot();
        var scatter = plot.Add.Scatter(xs, ys);
        scatter.Color = Colors.Red;
        scatter.LineWidth = 4;
        scatter.MarkerSize = 0;
        plot.Axes.SetLimits(0, 10, 0, 10);
        return plot;
    }

    private static bool IsLineRed(System.Drawing.Color c) => c.R > 200 && c.G < 80 && c.B < 80;

    // Cuenta columnas entre x0 y x1 (en coordenadas de datos) que contienen algún píxel rojo
    // en una banda de +-6 px alrededor de la fila de y = 5.
    private static int CountRedColumns(Bitmap bmp, Plot plot, double x0, double x1)
    {
        var p0 = plot.GetPixel(new Coordinates(x0, 5));
        var p1 = plot.GetPixel(new Coordinates(x1, 5));
        int left = (int)Math.Ceiling(Math.Min(p0.X, p1.X));
        int right = (int)Math.Floor(Math.Max(p0.X, p1.X));
        int row = (int)Math.Round(p0.Y);

        int count = 0;
        for (int x = left; x <= right; x++)
        {
            for (int y = row - 6; y <= row + 6; y++)
            {
                if (IsLineRed(bmp.GetPixel(x, y))) { count++; break; }
            }
        }
        return count;
    }

    [Fact]
    public void Spike_NaN_CortaLaLinea()
    {
        // Serie CON NaN en x = 5
        var plot = BuildPlot(withNaN: true);
        byte[] png = plot.GetImageBytes(Width, Height, ScottPlot.ImageFormat.Png);   // renderiza sin ventana
        using var bmp = new Bitmap(new MemoryStream(png));

        // Mismo trazado SIN NaN: control para demostrar que el método de medida detecta la continuidad.
        var plotNoNaN = BuildPlot(withNaN: false);
        byte[] pngNoNaN = plotNoNaN.GetImageBytes(Width, Height, ScottPlot.ImageFormat.Png);
        using var bmpNoNaN = new Bitmap(new MemoryStream(pngNoNaN));

        // Tramo con datos (x de 1,2 a 2,8) y tramo del NaN (x de 4,2 a 5,8; margen de 0,2 para no tocar los vecinos).
        int withData = CountRedColumns(bmp, plot, 1.2, 2.8);
        int inGap = CountRedColumns(bmp, plot, 4.2, 5.8);
        int controlGap = CountRedColumns(bmpNoNaN, plotNoNaN, 4.2, 5.8);
        int expectedColumns = (int)Math.Floor(plot.GetPixel(new Coordinates(5.8, 5)).X)
                            - (int)Math.Ceiling(plot.GetPixel(new Coordinates(4.2, 5)).X) + 1;

        Log($"[spike NaN] columnas medidas por tramo: {expectedColumns}");
        Log($"[spike NaN] con NaN    - tramo con datos (x 1,2..2,8): {withData} columnas con línea roja");
        Log($"[spike NaN] con NaN    - tramo del NaN   (x 4,2..5,8): {inGap} columnas con línea roja");
        Log($"[spike NaN] sin NaN    - mismo tramo     (x 4,2..5,8): {controlGap} columnas con línea roja (control)");

        Assert.True(withData > 0, "Control positivo fallido: no se detectó la línea en el tramo con datos.");
        Assert.True(controlGap > 0, "Control fallido: sin NaN la línea debería cruzar el tramo.");
        Assert.Equal(0, inGap);
    }

    [Fact]
    public void Spike_FormsPlot_SeCreaYSeDibujaEnUnFormReal()
    {
        Exception? error = null;
        int renderCountBefore = -1, renderCountAfter = -1;
        bool handleCreated = false;
        bool visible = false;

        var thread = new Thread(() =>
        {
            try
            {
                Application.EnableVisualStyles();
                using var form = new Form
                {
                    Text = "Spike FormsPlot",
                    Width = 640,
                    Height = 360,
                    StartPosition = FormStartPosition.CenterScreen,
                    ShowInTaskbar = false,
                };
                var formsPlot = new FormsPlot { Dock = DockStyle.Fill };
                form.Controls.Add(formsPlot);

                var scatter = formsPlot.Plot.Add.Scatter(
                    new double[] { 0, 1, 2, 3, 4, 5, 6 },
                    new double[] { 1, 2, double.NaN, 4, 5, 6, 7 });
                scatter.LineWidth = 3;
                formsPlot.Plot.Axes.SetLimits(0, 6, 0, 8);

                form.Show();                       // crea el handle del formulario y del control
                handleCreated = formsPlot.IsHandleCreated;
                visible = form.Visible;
                Application.DoEvents();

                renderCountBefore = formsPlot.Plot.RenderManager.RenderCount;
                formsPlot.Refresh();               // pide dibujar a través del control (SkiaSharp.Views.WindowsForms)
                Application.DoEvents();
                renderCountAfter = formsPlot.Plot.RenderManager.RenderCount;

                form.Close();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(TimeSpan.FromSeconds(60));

        Log($"[spike FormsPlot] error: {(error is null ? "ninguno" : error.ToString())}");
        Log($"[spike FormsPlot] handle creado: {handleCreated}; formulario visible: {visible}");
        Log($"[spike FormsPlot] RenderCount antes/después de Refresh: {renderCountBefore} / {renderCountAfter}");

        Assert.Null(error);
        Assert.True(handleCreated, "El control FormsPlot no creó su handle.");
        Assert.True(renderCountAfter > renderCountBefore, "Refresh no provocó ningún dibujo del control.");
    }
}
