namespace OptionsViewer.Core;

public static class MarketHours
{
    /// <summary>Apertura de la sesión, 09:30:00 (incluida).</summary>
    public static readonly TimeOnly Open = new(9, 30, 0);

    /// <summary>Cierre de la sesión, 16:00:00 (incluido).</summary>
    public static readonly TimeOnly Close = new(16, 0, 0);

    /// <summary>
    /// True si la hora está entre <see cref="Open"/> y <see cref="Close"/>, ambos incluidos. Compara la hora tal como
    /// viene en el archivo, sin conversión de zona (RNF-03).
    /// </summary>
    public static bool IsInSession(TimeOnly t) => Open <= t && t <= Close;
}
