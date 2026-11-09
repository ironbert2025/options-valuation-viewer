namespace OptionsViewer.Core;

public static class ChainFileName
{
    /// <summary>
    /// Busca "_Call_" o "_Put_" (sin distinguir mayúsculas) en el nombre del archivo; null si no está.
    /// Acepta el patrón con o sin &lt;id&gt; inicial. La fecha del nombre no se usa.
    /// </summary>
    public static OptionType? TryGetOptionType(string fileName)
    {
        if (fileName.Contains("_Call_", StringComparison.OrdinalIgnoreCase))
            return OptionType.Call;

        if (fileName.Contains("_Put_", StringComparison.OrdinalIgnoreCase))
            return OptionType.Put;

        return null;
    }
}
