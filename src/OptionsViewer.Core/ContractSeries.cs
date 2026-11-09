namespace OptionsViewer.Core;

/// <summary>Resultado de la gráfica 1: la serie de un contrato desde T0.</summary>
/// <param name="Key">Contrato (tipo y strike).</param>
/// <param name="T0">Momento de entrada.</param>
/// <param name="EntryAskPerContract">Ask(T0) × 100.</param>
/// <param name="PercentAvailable">False si Ask(T0) = 0 (el % es n/d).</param>
/// <param name="Points">Un punto por cada snapshot desde T0 hasta el último de la sesión.</param>
public record ContractSeries(
    ContractKey Key,
    TimeOnly T0,
    decimal EntryAskPerContract,
    bool PercentAvailable,
    IReadOnlyList<SeriesPoint> Points);
