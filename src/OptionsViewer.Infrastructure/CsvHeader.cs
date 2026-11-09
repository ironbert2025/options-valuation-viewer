using System.Diagnostics.CodeAnalysis;
using OptionsViewer.Core;

namespace OptionsViewer.Infrastructure;

/// <summary>
/// Cabecera del CSV: asigna a cada una de las 12 columnas esperadas su posición, leyendo por nombre
/// (sin distinguir mayúsculas, en cualquier orden e ignorando columnas de más). RF-01.
/// </summary>
internal sealed class CsvHeader
{
    private const int HeaderLine = 1;

    private readonly Dictionary<string, int> _indexByColumn;

    private CsvHeader(Dictionary<string, int> indexByColumn, int fieldCount)
    {
        _indexByColumn = indexByColumn;
        FieldCount = fieldCount;
    }

    /// <summary>Número de campos de la cabecera, incluidas las columnas de más.</summary>
    public int FieldCount { get; }

    /// <summary>Posición de una de las 12 columnas esperadas, por su nombre estándar (p. ej. "Ask").</summary>
    public int IndexOf(string column) => _indexByColumn[column];

    /// <summary>
    /// Lee la línea de cabecera. Si falta alguna de las 12 columnas devuelve <see cref="CsvErrorKind.MissingColumns"/>;
    /// si una de ellas se repite, <see cref="CsvErrorKind.DuplicateColumn"/>. Ambos con línea 1.
    /// Una columna de más repetida no es error.
    /// </summary>
    public static bool TryParse(
        string headerLine,
        [NotNullWhen(true)] out CsvHeader? header,
        [NotNullWhen(false)] out CsvError? error)
    {
        var fields = headerLine.TrimEnd('\r', '\n').Split(',');
        var indexByColumn = new Dictionary<string, int>(StringComparer.Ordinal);

        for (int i = 0; i < fields.Length; i++)
        {
            var expected = CsvColumns.All.FirstOrDefault(
                c => string.Equals(c, fields[i], StringComparison.OrdinalIgnoreCase));
            if (expected is null)
                continue;   // columna de más

            if (!indexByColumn.TryAdd(expected, i))
            {
                header = null;
                error = new CsvError(
                    CsvErrorKind.DuplicateColumn, HeaderLine, expected, fields[i], [],
                    $"Línea {HeaderLine}: la columna '{expected}' aparece más de una vez en la cabecera.");
                return false;
            }
        }

        var missing = CsvColumns.All.Where(c => !indexByColumn.ContainsKey(c)).ToList();
        if (missing.Count > 0)
        {
            header = null;
            error = new CsvError(
                CsvErrorKind.MissingColumns, HeaderLine, null, null, missing,
                $"Línea {HeaderLine}: faltan columnas en la cabecera: {string.Join(", ", missing)}.");
            return false;
        }

        header = new CsvHeader(indexByColumn, fields.Length);
        error = null;
        return true;
    }
}
