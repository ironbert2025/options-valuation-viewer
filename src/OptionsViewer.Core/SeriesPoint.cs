namespace OptionsViewer.Core;

/// <summary>Un punto de la serie: un snapshot desde T0. <c>Value == null</c> significa HUECO (contrato ausente de ese snapshot).</summary>
public record SeriesPoint(TimeOnly Time, PricePoint? Value)
{
    /// <summary>
    /// Posición en el eje horizontal en tiempo real (RF-06): <see cref="Time"/> en segundos desde 00:00:00.
    /// Se calcula siempre a partir de <see cref="Time"/>; no es un dato que se pase al construir el punto.
    /// </summary>
    public int SecondsOfDay => Time.Hour * 3600 + Time.Minute * 60 + Time.Second;
}
