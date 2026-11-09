namespace OptionsViewer.Core;

public record SeriesError(SeriesErrorKind Kind, TimeOnly T0, ContractKey Key);
