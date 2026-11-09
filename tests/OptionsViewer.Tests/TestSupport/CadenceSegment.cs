namespace OptionsViewer.Tests;

/// <summary>
/// Un tramo de un CSV sintético con cadencia propia: un snapshot cada <see cref="StepSeconds"/> segundos desde
/// <see cref="From"/> hasta <see cref="To"/> (ambas incluidas si el paso las alcanza). Varios tramos seguidos forman
/// un día con cadencia variable (p. ej. 6 s hasta las 11:00 y 1 minuto después), con tramos antes de 09:30:00
/// y después de 16:00:00 para probar también el filtro de horario.
/// </summary>
public sealed record CadenceSegment(TimeOnly From, TimeOnly To, int StepSeconds)
{
    public static CadenceSegment Of(string from, string to, int stepSeconds) =>
        new(TimeOnly.ParseExact(from, "HH:mm:ss"), TimeOnly.ParseExact(to, "HH:mm:ss"), stepSeconds);

    public IEnumerable<TimeOnly> Times()
    {
        int end = Seconds(To);
        for (int s = Seconds(From); s <= end; s += StepSeconds)
            yield return TimeOnly.FromTimeSpan(TimeSpan.FromSeconds(s));
    }

    /// <summary>Las horas de todos los tramos, en el orden dado.</summary>
    public static List<TimeOnly> AllTimes(IEnumerable<CadenceSegment> segments) =>
        segments.SelectMany(s => s.Times()).ToList();

    private static int Seconds(TimeOnly t) => (int)t.ToTimeSpan().TotalSeconds;
}
