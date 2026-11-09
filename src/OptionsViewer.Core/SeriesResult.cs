namespace OptionsViewer.Core;

/// <summary>Resultado tipado de construir la serie: o éxito o un <see cref="SeriesError"/>.</summary>
public record SeriesResult(ContractSeries? Value, SeriesError? Error)
{
    public bool IsSuccess => Error is null;
}
