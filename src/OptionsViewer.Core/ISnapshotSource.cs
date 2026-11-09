namespace OptionsViewer.Core;

/// <summary>Puerto de entrada de datos, definido en Core e implementado en Infrastructure.</summary>
public interface ISnapshotSource
{
    OptionType OptionType { get; }

    /// <summary>Nombre del archivo, para la interfaz.</summary>
    string DisplayName { get; }

    LoadResult Load(CancellationToken cancellationToken);
}
