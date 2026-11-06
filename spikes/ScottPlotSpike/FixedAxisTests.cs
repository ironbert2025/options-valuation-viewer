using System.Reflection;
using ScottPlot;
using ScottPlot.TickGenerators;
using ScottPlot.WinForms;

namespace ScottPlotSpike;

public class FixedAxisTests
{
    private const double Open = 9.5 * 3600;     // 09:30:00 = 34.200 s
    private const double Close = 16 * 3600;     // 16:00:00 = 57.600 s

    private static void Log(string line) =>
        File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "spike-output.txt"), line + Environment.NewLine);

    private static string Fmt(AxisLimits l) => $"X {l.Left:F1}..{l.Right:F1}  Y {l.Bottom:F2}..{l.Top:F2}";

    // Simula eventos reales del control invocando sus metodos protegidos OnMouse*
    private static void Raise(Control c, string method, MouseEventArgs e) =>
        typeof(Control).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(c, [e]);

    private static void Wheel(Control c, int x, int y, int delta)
    {
        Raise(c, "OnMouseMove", new MouseEventArgs(MouseButtons.None, 0, x, y, 0));
        Raise(c, "OnMouseWheel", new MouseEventArgs(MouseButtons.None, 0, x, y, delta));
    }

    private static void Drag(Control c, MouseButtons button, int x0, int y0, int x1, int y1)
    {
        Raise(c, "OnMouseMove", new MouseEventArgs(MouseButtons.None, 0, x0, y0, 0));
        Raise(c, "OnMouseDown", new MouseEventArgs(button, 1, x0, y0, 0));
        for (int i = 1; i <= 10; i++)
            Raise(c, "OnMouseMove", new MouseEventArgs(button, 0, x0 + (x1 - x0) * i / 10, y0 + (y1 - y0) * i / 10, 0));
        Raise(c, "OnMouseUp", new MouseEventArgs(button, 1, x1, y1, 0));
    }

    private static void MiddleClick(Control c, int x, int y)
    {
        Raise(c, "OnMouseMove", new MouseEventArgs(MouseButtons.None, 0, x, y, 0));
        Raise(c, "OnMouseDown", new MouseEventArgs(MouseButtons.Middle, 1, x, y, 0));
        Raise(c, "OnMouseUp", new MouseEventArgs(MouseButtons.Middle, 1, x, y, 0));
    }

    private static void DoubleClick(Control c, int x, int y)
    {
        Raise(c, "OnMouseMove", new MouseEventArgs(MouseButtons.None, 0, x, y, 0));
        Raise(c, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 2, x, y, 0));
        Raise(c, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 2, x, y, 0));
        Raise(c, "OnMouseDoubleClick", new MouseEventArgs(MouseButtons.Left, 2, x, y, 0));
    }

    private static void FixAxes(FormsPlot fp)
    {
        fp.Plot.Axes.SetLimitsX(Open, Close);
        fp.Plot.Axes.SetLimitsY(0, 10);
    }

    [Fact]
    public void Spike_EjeX_FijoSinZoom()
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { Run(); } catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(TimeSpan.FromSeconds(60));
        if (error is not null) Log($"[spike eje X] EXCEPCION: {error}");
        Assert.Null(error);
    }

    private void Run()
    {
        Application.EnableVisualStyles();
        using var form = new Form
        {
            Text = "Spike eje X", Width = 800, Height = 420,
            StartPosition = FormStartPosition.CenterScreen, ShowInTaskbar = false,
        };
        var fp = new FormsPlot { Dock = DockStyle.Fill };
        form.Controls.Add(fp);

        // Serie en segundos desde medianoche (X = SecondsOfDay), un punto cada 60 s de 09:30 a 16:00
        double[] xs = Enumerable.Range(0, 391).Select(i => Open + i * 60.0).ToArray();
        double[] ys = xs.Select(x => 5 + 4 * Math.Sin((x - Open) / 3000.0)).ToArray();
        fp.Plot.Add.Scatter(xs, ys);

        // Rotulos HH:mm sobre un eje numerico en segundos
        fp.Plot.Axes.Bottom.TickGenerator = new NumericAutomatic
        {
            LabelFormatter = v => TimeSpan.FromSeconds(v).ToString(@"hh\:mm"),
        };
        FixAxes(fp);

        form.Show();
        Application.DoEvents();
        fp.Refresh();
        Application.DoEvents();

        Log($"[spike eje X] DisplayScale del control: {fp.DisplayScale}; tamano cliente: {fp.Width}x{fp.Height}");
        // FormsPlot aloja un SKControl hijo que es quien recibe el raton y llama a los manejadores de ScottPlot
        var inner = (Control)typeof(FormsPlot).GetProperty("SKControl", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(fp)!;
        int outerMoves = 0;
        fp.MouseMove += (_, _) => outerMoves++;
        int cx = inner.Width / 2, cy = inner.Height / 2;
        Log($"[spike eje X] control interno: {inner.GetType().FullName} {inner.Width}x{inner.Height}");

        // ---- 1. Control positivo: con la interaccion por defecto, rueda y arrastre SI cambian los limites
        var l0 = fp.Plot.Axes.GetLimits();
        Wheel(inner, cx, cy, 120);
        Application.DoEvents();
        var afterWheelEnabled = fp.Plot.Axes.GetLimits();
        FixAxes(fp);
        Drag(inner, MouseButtons.Left, cx, cy, cx - 120, cy);
        Application.DoEvents();
        var afterDragEnabled = fp.Plot.Axes.GetLimits();
        Log($"[spike eje X] interaccion ACTIVA  - inicial: {Fmt(l0)}");
        Log($"[spike eje X] interaccion ACTIVA  - tras rueda:    {Fmt(afterWheelEnabled)}");
        Log($"[spike eje X] interaccion ACTIVA  - tras arrastre: {Fmt(afterDragEnabled)}");
        bool wheelWorks = afterWheelEnabled.Left != l0.Left || afterWheelEnabled.Right != l0.Right;
        bool dragWorks = afterDragEnabled.Left != l0.Left || afterDragEnabled.Right != l0.Right;
        Log($"[spike eje X] la simulacion de rueda cambia los limites: {wheelWorks}; la de arrastre: {dragWorks}");

        // ---- 2. Se desactiva la interaccion y se repite todo
        fp.UserInputProcessor.Disable();
        FixAxes(fp);
        fp.Refresh();
        Application.DoEvents();
        var fixedLimits = fp.Plot.Axes.GetLimits();

        for (int i = 0; i < 5; i++) { Wheel(inner, cx, cy, 120); Wheel(inner, cx, cy, -120); Wheel(inner, 50, cy, 120); }
        Drag(inner, MouseButtons.Left, cx, cy, cx - 200, cy + 40);
        Drag(inner, MouseButtons.Left, cx, cy, cx + 200, cy - 40);
        Drag(inner, MouseButtons.Right, cx, cy, cx + 150, cy - 50);
        Drag(inner, MouseButtons.Middle, cx, cy, cx - 100, cy + 20);
        MiddleClick(inner, cx, cy);
        DoubleClick(inner, cx, cy);
        Application.DoEvents();
        fp.Refresh();
        Application.DoEvents();
        var afterAll = fp.Plot.Axes.GetLimits();

        Log($"[spike eje X] interaccion DESACTIVADA - fijados: {Fmt(fixedLimits)}");
        Log($"[spike eje X] interaccion DESACTIVADA - tras 15 ruedas, 4 arrastres, clic central y doble clic: {Fmt(afterAll)}");
        Log($"[spike eje X] eventos MouseMove recibidos por el FormsPlot externo (tras disparar OnMouseMove en el control interno): {outerMoves}");

        // ---- 3. Conversion pixel -> coordenada con la zona de datos real del ultimo render
        var rect = fp.Plot.RenderManager.LastRender.DataRect;
        double yMid = (rect.Top + rect.Bottom) / 2;
        double xLeft = fp.Plot.GetCoordinates(new Pixel(rect.Left, yMid)).X;
        double xRight = fp.Plot.GetCoordinates(new Pixel(rect.Right, yMid)).X;
        double xMid = fp.Plot.GetCoordinates(new Pixel((rect.Left + rect.Right) / 2, yMid)).X;
        double xQuarter = fp.Plot.GetCoordinates(new Pixel(rect.Left + rect.Width / 4, yMid)).X;
        double secondsPerPixel = (Close - Open) / rect.Width;

        Log($"[spike eje X] DataRect: left={rect.Left:F1} right={rect.Right:F1} width={rect.Width:F1}; {secondsPerPixel:F1} s por pixel");
        Log($"[spike eje X] pixel izquierdo -> X={xLeft:F1} (esperado {Open}); pixel derecho -> X={xRight:F1} (esperado {Close})");
        Log($"[spike eje X] pixel central -> X={xMid:F1} ({TimeSpan.FromSeconds(xMid):hh\\:mm\\:ss}; esperado {(Open + Close) / 2} = 12:45:00); a 1/4 -> X={xQuarter:F1} ({TimeSpan.FromSeconds(xQuarter):hh\\:mm\\:ss}; esperado {Open + (Close - Open) / 4} = 11:07:30)");

        // Conversion inversa: coordenada -> pixel
        var pxOpen = fp.Plot.GetPixel(new Coordinates(Open, 5));
        var pxClose = fp.Plot.GetPixel(new Coordinates(Close, 5));
        Log($"[spike eje X] GetPixel(09:30:00).X={pxOpen.X:F1} (DataRect.Left={rect.Left:F1}); GetPixel(16:00:00).X={pxClose.X:F1} (DataRect.Right={rect.Right:F1})");

        // ---- 4. Rotulos del eje inferior
        var ticks = fp.Plot.Axes.Bottom.TickGenerator.Ticks.Where(t => t.IsMajor).ToArray();
        Log($"[spike eje X] rotulos mayores del eje inferior ({ticks.Length}): {string.Join(" ", ticks.Select(t => t.Label))}");

        // ---- 5. Rotulos manuales cada 30 min (09:30, 10:00, ..., 16:00)
        double[] pos = Enumerable.Range(0, 14).Select(i => Open + i * 1800.0).ToArray();
        string[] labels = pos.Select(v => TimeSpan.FromSeconds(v).ToString(@"hh\:mm")).ToArray();
        fp.Plot.Axes.Bottom.TickGenerator = new NumericManual(pos, labels);
        fp.Refresh();
        Application.DoEvents();
        var manualTicks = fp.Plot.Axes.Bottom.TickGenerator.Ticks.Where(t => t.IsMajor).ToArray();
        Log($"[spike eje X] rotulos manuales cada 30 min ({manualTicks.Length}): {string.Join(" ", manualTicks.Select(t => t.Label))}");

        form.Close();

        Assert.Equal(14, manualTicks.Length);
        Assert.Equal("09:30", manualTicks.First().Label);
        Assert.Equal("16:00", manualTicks.Last().Label);
        Assert.True(wheelWorks, "Control positivo fallido: la rueda simulada no cambia los limites con la interaccion activa.");
        Assert.True(dragWorks, "Control positivo fallido: el arrastre simulado no cambia los limites con la interaccion activa.");
        Assert.Equal(Open, fixedLimits.Left, 3);
        Assert.Equal(Close, fixedLimits.Right, 3);
        Assert.Equal(fixedLimits.Left, afterAll.Left, 3);
        Assert.Equal(fixedLimits.Right, afterAll.Right, 3);
        Assert.Equal(fixedLimits.Bottom, afterAll.Bottom, 3);
        Assert.Equal(fixedLimits.Top, afterAll.Top, 3);
        Assert.InRange(xLeft, Open - secondsPerPixel, Open + secondsPerPixel);
        Assert.InRange(xRight, Close - secondsPerPixel, Close + secondsPerPixel);
        Assert.InRange(xMid, (Open + Close) / 2 - secondsPerPixel, (Open + Close) / 2 + secondsPerPixel);
        Assert.InRange(xQuarter, Open + (Close - Open) / 4 - secondsPerPixel, Open + (Close - Open) / 4 + secondsPerPixel);
        Assert.All(ticks, t => Assert.Matches(@"^\d\d:\d\d$", t.Label));
    }
}
