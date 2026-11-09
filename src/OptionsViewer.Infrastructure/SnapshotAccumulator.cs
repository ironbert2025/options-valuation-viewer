using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using OptionsViewer.Core;

namespace OptionsViewer.Infrastructure;

/// <summary>
/// Agrupa filas ya convertidas en snapshots: filas consecutivas con la misma <c>Time</c> forman un snapshot (RF-02).
/// Detecta una <c>Time</c> que retrocede (<see cref="CsvErrorKind.OutOfOrder"/>) y un strike repetido dentro del
/// mismo snapshot (<see cref="CsvErrorKind.DuplicateStrike"/>). No usa ninguna diferencia de tiempo entre snapshots
/// (RNF-10): lo único que importa es si la <c>Time</c> cambia.
/// </summary>
internal sealed class SnapshotAccumulator
{
    private readonly List<Snapshot> _snapshots = [];
    private TimeOnly? _currentTime;
    private Dictionary<decimal, ContractQuote> _currentQuotes = [];

    public bool TryAdd(ParsedRow row, int lineNumber, [NotNullWhen(false)] out CsvError? error)
    {
        if (_currentTime is { } current)
        {
            if (row.Time < current)
            {
                var time = row.Time.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
                error = new CsvError(
                    CsvErrorKind.OutOfOrder, lineNumber, CsvColumns.Time, time, [],
                    $"Línea {lineNumber}: la hora {time} es anterior a la del snapshot anterior "
                    + $"({current.ToString("HH:mm:ss", CultureInfo.InvariantCulture)}); las filas deben venir ordenadas por hora.");
                return false;
            }

            if (row.Time > current)
                CloseCurrent();
        }

        _currentTime = row.Time;

        if (!_currentQuotes.TryAdd(row.Quote.Strike, row.Quote))
        {
            var strike = row.Quote.Strike.ToString(CultureInfo.InvariantCulture);
            error = new CsvError(
                CsvErrorKind.DuplicateStrike, lineNumber, CsvColumns.StrikePrice, strike, [],
                $"Línea {lineNumber}: el strike {strike} aparece más de una vez en el snapshot de las "
                + $"{row.Time.ToString("HH:mm:ss", CultureInfo.InvariantCulture)}.");
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>Cierra el snapshot en curso y devuelve todos los acumulados, en orden de llegada.</summary>
    public IReadOnlyList<Snapshot> Build()
    {
        CloseCurrent();
        return _snapshots;
    }

    private void CloseCurrent()
    {
        if (_currentTime is { } time && _currentQuotes.Count > 0)
            _snapshots.Add(new Snapshot(time, _currentQuotes));

        _currentTime = null;
        _currentQuotes = [];
    }
}
