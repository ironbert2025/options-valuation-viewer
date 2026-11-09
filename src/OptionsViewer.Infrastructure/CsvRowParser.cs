using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using OptionsViewer.Core;

namespace OptionsViewer.Infrastructure;

/// <summary>Una fila del CSV ya convertida: su <c>Time</c> y las 11 columnas numéricas.</summary>
internal readonly record struct ParsedRow(TimeOnly Time, ContractQuote Quote);

/// <summary>
/// Convierte una fila de texto en <see cref="ParsedRow"/> usando la posición de cada columna que da el
/// <see cref="CsvHeader"/>. Todas las conversiones usan <see cref="CultureInfo.InvariantCulture"/> de forma explícita
/// (punto decimal, independiente de la configuración regional del equipo). RF-01, RNF-09.
/// </summary>
internal static class CsvRowParser
{
    private const NumberStyles DecimalStyles = NumberStyles.Float;

    public static bool TryParse(
        CsvHeader header,
        string line,
        int lineNumber,
        out ParsedRow row,
        [NotNullWhen(false)] out CsvError? error)
    {
        row = default;

        // RNF-09: un retorno de carro pegado al último campo no cuenta como parte del valor
        var fields = line.TrimEnd('\r', '\n').Split(',');

        if (fields.Length < header.FieldCount)
        {
            error = new CsvError(
                CsvErrorKind.MalformedRow, lineNumber, null, null, [],
                $"Línea {lineNumber}: la fila tiene {fields.Length} campos y la cabecera tiene {header.FieldCount}.");
            return false;
        }

        var rawTime = fields[header.IndexOf(CsvColumns.Time)];
        if (!TimeOnly.TryParseExact(rawTime, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            error = new CsvError(
                CsvErrorKind.InvalidValue, lineNumber, CsvColumns.Time, rawTime, [],
                $"Línea {lineNumber}: la hora '{rawTime}' de la columna {CsvColumns.Time} no tiene el formato HH:MM:SS.");
            return false;
        }

        var values = new decimal[CsvColumns.All.Count];
        for (int i = 1; i < CsvColumns.All.Count; i++)   // la columna 0 es Time
        {
            var column = CsvColumns.All[i];
            var raw = fields[header.IndexOf(column)];
            if (!decimal.TryParse(raw, DecimalStyles, CultureInfo.InvariantCulture, out values[i]))
            {
                error = new CsvError(
                    CsvErrorKind.InvalidValue, lineNumber, column, raw, [],
                    $"Línea {lineNumber}: valor no numérico '{raw}' en la columna {column}.");
                return false;
            }
        }

        // Orden de CsvColumns.All: Time, SpotPrice, StrikePrice, Bid, Ask, IntValue, ExtValue, Delta, Gamma, Theta, Vega, IV
        var quote = new ContractQuote(
            Strike: values[2], SpotPrice: values[1], Bid: values[3], Ask: values[4],
            IntValue: values[5], ExtValue: values[6],
            Delta: values[7], Gamma: values[8], Theta: values[9], Vega: values[10], IV: values[11]);

        row = new ParsedRow(time, quote);
        error = null;
        return true;
    }
}
