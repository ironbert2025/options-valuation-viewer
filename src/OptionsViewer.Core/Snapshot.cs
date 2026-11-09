namespace OptionsViewer.Core;

/// <summary>Todas las filas con la misma <c>Time</c> (RF-02). Strike único dentro del snapshot.</summary>
public record Snapshot(TimeOnly Time, IReadOnlyDictionary<decimal, ContractQuote> QuotesByStrike);
