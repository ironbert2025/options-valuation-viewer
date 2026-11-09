using System.Globalization;
using OptionsViewer.Core;
using OptionsViewer.Infrastructure;

namespace OptionsViewer.Tests;

public class CsvRowParserTests
{
    // Filas reales de samples/
    private const string CallsRow340 = "09:30:00,341.53,340.0,2.89,3.0,1.53,1.47,0.625,0.193,-0.325,0.028,42.582";
    private const string PutsLastRow = "18:24:35,350.13,350.0,0.28,0.56,-0.13,0.48,1.0,0.0,0.0,0.0,-999.0";

    private static CsvHeader StandardHeader()
    {
        Assert.True(CsvHeader.TryParse(string.Join(",", SyntheticCsv.StandardColumns), out var header, out _));
        return header;
    }

    private static CsvHeader HeaderOf(params string[] columns)
    {
        Assert.True(CsvHeader.TryParse(string.Join(",", columns), out var header, out _));
        return header;
    }

    [Fact]
    [Trait("RF", "RF-01")]
    [Trait("CA", "CA-02")]
    public void Parse_FilaValida_ConvierteDecimalesConPunto()
    {
        // Con una cultura que usa coma decimal, "341.53" no debe leerse con la cultura actual.
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("es-ES");
        try
        {
            Assert.Equal(',', CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0]);   // el rojo depende de esto

            Assert.True(CsvRowParser.TryParse(StandardHeader(), CallsRow340, 2, out var row, out var error));
            Assert.Null(error);
            Assert.Equal(new TimeOnly(9, 30, 0), row.Time);
            Assert.Equal(
                new ContractQuote(
                    Strike: 340.0m, SpotPrice: 341.53m, Bid: 2.89m, Ask: 3.0m,
                    IntValue: 1.53m, ExtValue: 1.47m,
                    Delta: 0.625m, Gamma: 0.193m, Theta: -0.325m, Vega: 0.028m, IV: 42.582m),
                row.Quote);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    [Trait("RF", "RF-01")]
    [Trait("CA", "CA-01")]
    [Trait("CA", "CA-02")]
    public void Parse_IVConCRFinal_LeeNumero()
    {
        // RNF-09: un \r pegado al último campo (IV) no debe romper la lectura.
        Assert.True(CsvRowParser.TryParse(StandardHeader(), CallsRow340 + "\r", 2, out var row, out var error));
        Assert.Null(error);
        Assert.Equal(42.582m, row.Quote.IV);
    }

    [Fact]
    [Trait("RF", "RF-01")]
    [Trait("CA", "CA-01")]
    public void Parse_TimeUltimaColumnaConCR_LeeHora()
    {
        var header = HeaderOf("SpotPrice", "StrikePrice", "Bid", "Ask", "IntValue", "ExtValue", "Delta", "Gamma", "Theta", "Vega", "IV", "Time");
        var line = "341.53,340.0,2.89,3.0,1.53,1.47,0.625,0.193,-0.325,0.028,42.582,09:30:00\r";

        Assert.True(CsvRowParser.TryParse(header, line, 2, out var row, out var error));
        Assert.Null(error);
        Assert.Equal(new TimeOnly(9, 30, 0), row.Time);
    }

    [Fact]
    [Trait("RF", "RF-01")]
    [Trait("CA", "CA-01")]
    public void Parse_IVMenos999_LeeNumero()
    {
        // Última fila real de los Puts de referencia: IV = -999.0 (fuera de horario)
        Assert.True(CsvRowParser.TryParse(StandardHeader(), PutsLastRow + "\r", 17367, out var row, out var error));
        Assert.Null(error);
        Assert.Equal(new TimeOnly(18, 24, 35), row.Time);
        Assert.Equal(-999.0m, row.Quote.IV);
    }

    [Fact]
    [Trait("RF", "RF-01")]
    [Trait("CA", "CA-02")]
    public void Parse_ColumnasReordenadasYExtra_LeeCadaCampoPorNombre()
    {
        var header = HeaderOf("Ask", "Extra", "Time", "Bid", "StrikePrice", "SpotPrice", "IntValue", "ExtValue", "Delta", "Gamma", "Theta", "Vega", "IV");
        var line = "3.0,zzz,09:30:00,2.89,340.0,341.53,1.53,1.47,0.625,0.193,-0.325,0.028,42.582";

        Assert.True(CsvRowParser.TryParse(header, line, 2, out var row, out var error));
        Assert.Null(error);
        Assert.Equal(new TimeOnly(9, 30, 0), row.Time);
        Assert.Equal(3.0m, row.Quote.Ask);
        Assert.Equal(2.89m, row.Quote.Bid);
        Assert.Equal(340.0m, row.Quote.Strike);
        Assert.Equal(341.53m, row.Quote.SpotPrice);
    }

    [Fact]
    [Trait("RF", "RF-01")]
    [Trait("CA", "CA-02")]
    public void Parse_ValorNoNumerico_InvalidValueConColumnaYLinea()
    {
        var line = CallsRow340.Replace(",3.0,", ",abc,");

        Assert.False(CsvRowParser.TryParse(StandardHeader(), line, 7, out _, out var error));
        Assert.Equal(CsvErrorKind.InvalidValue, error.Kind);
        Assert.Equal(7, error.Line);
        Assert.Equal("Ask", error.Column);
        Assert.Equal("abc", error.RawValue);
        Assert.Contains("7", error.Message);
        Assert.Contains("Ask", error.Message);
    }

    [Fact]
    [Trait("RF", "RF-01")]
    [Trait("CA", "CA-02")]
    public void Parse_CeldaVacia_InvalidValue()
    {
        var line = CallsRow340.Replace(",3.0,", ",,");

        Assert.False(CsvRowParser.TryParse(StandardHeader(), line, 5, out _, out var error));
        Assert.Equal(CsvErrorKind.InvalidValue, error.Kind);
        Assert.Equal(5, error.Line);
        Assert.Equal("Ask", error.Column);
        Assert.Equal("", error.RawValue);
    }

    [Theory]
    [InlineData("9:30")]
    [InlineData("09:30")]
    [InlineData("25:00:00")]
    [InlineData("09:30:00.5")]
    [InlineData("hola")]
    [Trait("RF", "RF-01")]
    [Trait("CA", "CA-02")]
    public void Parse_TimeMalFormado_InvalidValue(string badTime)
    {
        var line = badTime + CallsRow340["09:30:00".Length..];

        Assert.False(CsvRowParser.TryParse(StandardHeader(), line, 9, out _, out var error));
        Assert.Equal(CsvErrorKind.InvalidValue, error.Kind);
        Assert.Equal(9, error.Line);
        Assert.Equal("Time", error.Column);
        Assert.Equal(badTime, error.RawValue);
    }

    [Fact]
    [Trait("RF", "RF-01")]
    [Trait("CA", "CA-02")]
    public void Parse_MenosCampos_MalformedRow()
    {
        var line = "09:30:00,341.53,340.0,2.89";

        Assert.False(CsvRowParser.TryParse(StandardHeader(), line, 11, out _, out var error));
        Assert.Equal(CsvErrorKind.MalformedRow, error.Kind);
        Assert.Equal(11, error.Line);
        Assert.Contains("11", error.Message);
    }
}
