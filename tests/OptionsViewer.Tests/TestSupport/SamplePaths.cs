namespace OptionsViewer.Tests;

/// <summary>
/// Rutas de los CSV de referencia. Se enlazan desde <c>samples/</c> y se copian a la carpeta de salida
/// (<c>samples/</c> junto al ejecutable de pruebas); no se duplican dentro de <c>tests/</c>.
/// </summary>
public static class SamplePaths
{
    public static string Calls { get; } = Path.Combine(AppContext.BaseDirectory, "samples", "AAPL_Call_20261104_20261104.csv");

    public static string Puts { get; } = Path.Combine(AppContext.BaseDirectory, "samples", "AAPL_Put_20261104_20261104.csv");
}
