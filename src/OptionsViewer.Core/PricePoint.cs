namespace OptionsViewer.Core;

/// <summary>Valores de un contrato en un snapshot, todo por 1 contrato (precio cotizado × 100).</summary>
/// <param name="AskPerContract">Ask × 100.</param>
/// <param name="BidPerContract">Bid × 100.</param>
/// <param name="PnlDollars">Bid_c(t) − Ask_c(T0).</param>
/// <param name="PnlPercent">Null ⇒ n/d (solo cuando Ask_c(T0) = 0).</param>
public record PricePoint(
    decimal AskPerContract,
    decimal BidPerContract,
    decimal PnlDollars,
    decimal? PnlPercent);
