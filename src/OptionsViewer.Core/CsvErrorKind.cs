namespace OptionsViewer.Core;

public enum CsvErrorKind
{
    EmptyFile,
    MissingColumns,
    DuplicateColumn,
    MalformedRow,
    InvalidValue,
    OutOfOrder,
    DuplicateStrike,
}
