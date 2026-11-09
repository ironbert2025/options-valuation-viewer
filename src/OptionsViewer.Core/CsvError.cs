namespace OptionsViewer.Core;

/// <summary>Error de contenido al leer el CSV (RF-01, CA-02).</summary>
/// <param name="Kind">Tipo de error.</param>
/// <param name="Line">Número de línea física, base 1, la cabecera es la línea 1; null en EmptyFile de 0 bytes.</param>
/// <param name="Column">Nombre de columna afectada, si aplica.</param>
/// <param name="RawValue">Texto original que falló, si aplica.</param>
/// <param name="MissingColumns">Nombres de las columnas que faltan (MissingColumns).</param>
/// <param name="Message">Texto en español listo para mostrar.</param>
public record CsvError(
    CsvErrorKind Kind,
    int? Line,
    string? Column,
    string? RawValue,
    IReadOnlyList<string> MissingColumns,
    string Message);
