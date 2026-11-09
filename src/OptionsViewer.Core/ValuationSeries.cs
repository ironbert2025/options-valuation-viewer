namespace OptionsViewer.Core;

public static class ValuationSeries
{
    /// <summary>
    /// Construye la serie del contrato desde T0 (RF-04..06). Por ahora solo valida la entrada (T-4.3):
    /// los errores de contenido se devuelven tipados en <see cref="SeriesResult.Error"/> con <c>Value = null</c>;
    /// un tipo de contrato distinto del de la sesión es un error de programación y lanza <see cref="ArgumentException"/>.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="key"/> es de un tipo distinto al de la sesión.</exception>
    public static SeriesResult Build(TradingSession session, ContractKey key, TimeOnly t0)
    {
        if (key.Type != session.Type)
            throw new ArgumentException(
                $"El contrato es de tipo {key.Type} pero la sesión es de tipo {session.Type}.", nameof(key));

        if (session.Snapshots.Count == 0)
            return Fail(SeriesErrorKind.EmptySession, t0, key);

        var snapshot = session.Snapshots.FirstOrDefault(s => s.Time == t0);
        if (snapshot is null)
            return Fail(SeriesErrorKind.T0NotASnapshot, t0, key);

        if (!snapshot.QuotesByStrike.ContainsKey(key.Strike))
            return Fail(SeriesErrorKind.ContractAbsentAtT0, t0, key);

        // El cálculo de la serie (entrada, Bid, PnL y huecos) se implementa en T-4.4 y T-4.5.
        throw new NotImplementedException("La construcción de la serie con datos se implementa en T-4.4.");
    }

    private static SeriesResult Fail(SeriesErrorKind kind, TimeOnly t0, ContractKey key) =>
        new(null, new SeriesError(kind, t0, key));
}
