namespace OptionsViewer.Core;

/// <summary>Resultado tipado de cargar un archivo: o éxito o un <see cref="CsvError"/>.</summary>
public record LoadResult(SnapshotSet? Value, CsvError? Error)
{
    public bool IsSuccess => Error is null;
}
