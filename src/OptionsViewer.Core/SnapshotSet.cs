namespace OptionsViewer.Core;

/// <summary>Resultado de leer el archivo: todo el contenido, sin filtrar. Orden estrictamente creciente por <c>Time</c>.</summary>
public record SnapshotSet(OptionType Type, IReadOnlyList<Snapshot> Snapshots);
