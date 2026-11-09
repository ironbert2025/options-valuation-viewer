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

    public string Build()
    {
        var sb = new StringBuilder();
        sb.Append(string.Join(",", _columns)).Append(_lineEnding);
        foreach (var row in _rows)
            sb.Append(string.Join(",", row)).Append(_lineEnding);
        return sb.ToString();
    }
}
