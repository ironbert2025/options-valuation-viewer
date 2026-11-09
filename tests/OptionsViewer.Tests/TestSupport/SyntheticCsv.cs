using System.Text;

namespace OptionsViewer.Tests;

/// <summary>
/// Constructor de CSV sintéticos para las pruebas: cabecera, orden de columnas, fin de línea (CRLF por defecto)
/// y filas dados. Cada línea, la última incluida, termina con el fin de línea, como en los archivos de referencia.
/// </summary>
public sealed class SyntheticCsv
{
    public static readonly string[] StandardColumns =
        ["Time", "SpotPrice", "StrikePrice", "Bid", "Ask", "IntValue", "ExtValue", "Delta", "Gamma", "Theta", "Vega", "IV"];

    private string[] _columns = StandardColumns;
    private string _lineEnding = "\r\n";
    private readonly List<string[]> _rows = [];

    public SyntheticCsv WithColumns(params string[] columns)
    {
        _columns = columns;
        return this;
    }

    public SyntheticCsv WithLineEnding(string lineEnding)
    {
        _lineEnding = lineEnding;
        return this;
    }

    public SyntheticCsv AddRow(params string[] fields)
    {
        _rows.Add(fields);
        return this;
    }

    /// <summary>
    /// CSV sintético con un snapshot por cada hora de <paramref name="times"/> (cadencia libre, p. ej. de
    /// <see cref="CadenceSegment"/>). Cada snapshot trae el strike 340,0 (siempre presente) y el 342,5, que falta en
    /// las horas de <paramref name="withoutSecondStrike"/> para definir huecos. Bid y Ask varían con la hora
    /// (Bid = Ask − 0,10, siempre positivo).
    /// </summary>
    public static SyntheticCsv FromTimes(IEnumerable<TimeOnly> times, IReadOnlySet<TimeOnly>? withoutSecondStrike = null)
    {
        var csv = new SyntheticCsv();
        foreach (var time in times)
        {
            int seconds = (int)time.ToTimeSpan().TotalSeconds;
            decimal ask = 3.00m + (seconds % 7) * 0.05m;
            decimal bid = ask - 0.10m;

            csv.AddRow(QuoteRow(time, "340.0", bid, ask));
            if (withoutSecondStrike is null || !withoutSecondStrike.Contains(time))
                csv.AddRow(QuoteRow(time, "342.5", bid, ask));
        }
        return csv;
    }

    private static string[] QuoteRow(TimeOnly time, string strike, decimal bid, decimal ask)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        return
        [
            time.ToString("HH:mm:ss", inv), "341.53", strike, bid.ToString("0.00", inv), ask.ToString("0.00", inv),
            "1.53", "1.47", "0.625", "0.193", "-0.325", "0.028", "42.582",
        ];
    }

    public string Build()
    {
        var sb = new StringBuilder();
        sb.Append(string.Join(",", _columns)).Append(_lineEnding);
        foreach (var row in _rows)
            sb.Append(string.Join(",", row)).Append(_lineEnding);
        return sb.ToString();
    }
}
