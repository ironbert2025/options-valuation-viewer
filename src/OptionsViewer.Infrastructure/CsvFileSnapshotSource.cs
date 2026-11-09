using System.Text;
using OptionsViewer.Core;

namespace OptionsViewer.Infrastructure;

/// <summary>
/// Fuente de snapshots que lee un CSV completo desde disco (modo offline).
/// Abre el archivo solo para lectura y permitiendo que otro proceso lo siga escribiendo
/// (<see cref="FileAccess.Read"/> + <see cref="FileShare.ReadWrite"/>, RNF-01): no lo bloquea ni lo modifica.
/// Los errores de contenido se devuelven en el <see cref="LoadResult"/>; los de entorno (archivo inexistente,
/// permisos, E/S) se propagan como excepciones, y la cancelación como <see cref="OperationCanceledException"/>.
/// </summary>
public sealed class CsvFileSnapshotSource : ISnapshotSource
{
    private readonly string _path;

    public CsvFileSnapshotSource(string path, OptionType type)
    {
        _path = path;
        OptionType = type;
    }

    public OptionType OptionType { get; }

    public string DisplayName => Path.GetFileName(_path);

    public LoadResult Load(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        // Cabecera (línea 1)
        var headerLine = reader.ReadLine();
        if (headerLine is null)
            return Fail(new CsvError(CsvErrorKind.EmptyFile, null, null, null, [], "El archivo está vacío."));

        if (!CsvHeader.TryParse(headerLine, out var header, out var headerError))
            return Fail(headerError);

        // Filas de datos (desde la línea 2); las líneas en blanco se ignoran pero cuentan en la numeración
        var accumulator = new SnapshotAccumulator();
        int lineNumber = 1;
        int dataRows = 0;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (!CsvRowParser.TryParse(header, line, lineNumber, out var row, out var rowError))
                return Fail(rowError);

            if (!accumulator.TryAdd(row, lineNumber, out var groupError))
                return Fail(groupError);

            dataRows++;
        }

        if (dataRows == 0)
            return Fail(new CsvError(CsvErrorKind.EmptyFile, 1, null, null, [], "El archivo solo tiene la cabecera, sin filas de datos."));

        return new LoadResult(new SnapshotSet(OptionType, accumulator.Build()), null);
    }

    private static LoadResult Fail(CsvError error) => new(null, error);
}
