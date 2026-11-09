namespace OptionsViewer.Core;

public static class ValuationSeries
{
    /// <summary>
    /// Construye la serie del contrato desde T0 (RF-04..06): un punto por cada snapshot de la sesión desde T0.
    /// Los errores de contenido se devuelven tipados en <see cref="SeriesResult.Error"/> con <c>Value = null</c>;
    /// un tipo de contrato distinto del de la sesión es un error de programación y lanza <see cref="ArgumentException"/>.
    /// Los precios son por contrato (cotizado × 100), en decimal y sin redondear.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="key"/> es de un tipo distinto al de la sesión.</exception>
    public static SeriesResult Build(TradingSession session, ContractKey key, TimeOnly t0)
    {
        if (key.Type != session.Type)
            throw new ArgumentException(
                $"El contrato es de tipo {key.Type} pero la sesión es de tipo {session.Type}.", nameof(key));

        if (session.Snapshots.Count == 0)
            return Fail(SeriesErrorKind.EmptySession, t0, key);

        var snapshots = session.Snapshots;
        int startIndex = -1;
        for (int i = 0; i < snapshots.Count; i++)
        {
            if (snapshots[i].Time == t0)
            {
                startIndex = i;
                break;
            }
        }

        if (startIndex < 0)
            return Fail(SeriesErrorKind.T0NotASnapshot, t0, key);

        if (!snapshots[startIndex].QuotesByStrike.TryGetValue(key.Strike, out var entryQuote))
            return Fail(SeriesErrorKind.ContractAbsentAtT0, t0, key);

        // Precio de entrada por contrato: Ask(T0) × 100 (decimal, sin redondeo)
        decimal entryAskPerContract = entryQuote.Ask * 100m;

        var points = new List<SeriesPoint>();
        for (int i = startIndex; i < snapshots.Count; i++)
        {
            var snapshot = snapshots[i];
            if (!snapshot.QuotesByStrike.TryGetValue(key.Strike, out var quote))
                continue;   // los huecos (contrato ausente de un snapshot) se emiten en T-4.5

            points.Add(new SeriesPoint(snapshot.Time, PriceAt(quote, entryAskPerContract)));
        }

        // PercentAvailable fijo en true hasta T-4.6 (caso Ask(T0) = 0)
        return new SeriesResult(new ContractSeries(key, t0, entryAskPerContract, PercentAvailable: true, points), null);
    }

    private static PricePoint PriceAt(ContractQuote quote, decimal entryAskPerContract)
    {
        decimal askPerContract = quote.Ask * 100m;
        decimal bidPerContract = quote.Bid * 100m;
        decimal pnlDollars = bidPerContract - entryAskPerContract;
        decimal pnlPercent = pnlDollars / entryAskPerContract * 100m;   // sin redondeo; el redondeo es solo al mostrar
        return new PricePoint(askPerContract, bidPerContract, pnlDollars, pnlPercent);
    }

    private static SeriesResult Fail(SeriesErrorKind kind, TimeOnly t0, ContractKey key) =>
        new(null, new SeriesError(kind, t0, key));
}
