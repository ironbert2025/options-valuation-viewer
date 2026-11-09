namespace OptionsViewer.Infrastructure;

/// <summary>Las 12 columnas esperadas del CSV (RF-01), en el orden estándar.</summary>
internal static class CsvColumns
{
    public const string Time = "Time";
    public const string SpotPrice = "SpotPrice";
    public const string StrikePrice = "StrikePrice";
    public const string Bid = "Bid";
    public const string Ask = "Ask";
    public const string IntValue = "IntValue";
    public const string ExtValue = "ExtValue";
    public const string Delta = "Delta";
    public const string Gamma = "Gamma";
    public const string Theta = "Theta";
    public const string Vega = "Vega";
    public const string IV = "IV";

    public static readonly IReadOnlyList<string> All =
        [Time, SpotPrice, StrikePrice, Bid, Ask, IntValue, ExtValue, Delta, Gamma, Theta, Vega, IV];
}
