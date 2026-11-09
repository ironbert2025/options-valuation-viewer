namespace OptionsViewer.Core;

/// <summary>Resultado del filtro de horario (RF-02, RF-10).</summary>
/// <param name="Type">Tipo de opción del archivo.</param>
/// <param name="Snapshots">Solo los snapshots entre 09:30:00 y 16:00:00, ambos incluidos.</param>
/// <param name="DiscardedCount">Snapshots descartados por caer fuera de horario (CA-01: 2 en los archivos de referencia).</param>
/// <param name="NoSnapshotsInWindow">True si no queda ningún snapshot dentro del horario (RF-10 / CA-12).</param>
public record TradingSession(
    OptionType Type,
    IReadOnlyList<Snapshot> Snapshots,
    int DiscardedCount,
    bool NoSnapshotsInWindow);
