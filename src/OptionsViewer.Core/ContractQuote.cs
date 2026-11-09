namespace OptionsViewer.Core;

/// <summary>Una fila del CSV (RF-01: las 12 columnas; <c>Time</c> vive en el <see cref="Snapshot"/>).</summary>
public record ContractQuote(
    decimal Strike, decimal SpotPrice, decimal Bid, decimal Ask,
    decimal IntValue, decimal ExtValue,
    decimal Delta, decimal Gamma, decimal Theta, decimal Vega, decimal IV);
