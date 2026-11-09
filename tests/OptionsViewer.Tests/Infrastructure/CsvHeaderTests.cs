using OptionsViewer.Core;
using OptionsViewer.Infrastructure;

namespace OptionsViewer.Tests;

public class CsvHeaderTests
{
    private static readonly string[] Standard = SyntheticCsv.StandardColumns;

    private static string Line(IEnumerable<string> columns) => string.Join(",", columns);

    [Fact]
    [Trait("RF", "RF-01")]
    [Trait("CA", "CA-02")]
    public void Parse_Cabecera12Columnas_MapeaTodas()
    {
        Assert.True(CsvHeader.TryParse(Line(Standard), out var header, out var error));
        Assert.Null(error);
        Assert.Equal(12, header.FieldCount);
        for (int i = 0; i < Standard.Length; i++)
            Assert.Equal(i, header.IndexOf(Standard[i]));
    }

    [Fact]
    [Trait("RF", "RF-01")]
    [Trait("CA", "CA-02")]
    public void Parse_OtroOrdenYMinusculas_Acepta()
    {
        var line = "ask,time,STRIKEPRICE,bid,spotprice,IntValue,extvalue,DELTA,gamma,theta,Vega,iv";

        Assert.True(CsvHeader.TryParse(line, out var header, out var error));
        Assert.Null(error);
        Assert.Equal(0, header.IndexOf("Ask"));
        Assert.Equal(1, header.IndexOf("Time"));
        Assert.Equal(2, header.IndexOf("StrikePrice"));
        Assert.Equal(3, header.IndexOf("Bid"));
        Assert.Equal(11, header.IndexOf("IV"));
    }

    [Fact]
    [Trait("RF", "RF-01")]
    [Trait("CA", "CA-02")]
    public void Parse_ColumnaDeMas_Ignora()
    {
        var columns = Standard.ToList();
        columns.Insert(5, "Extra");
        columns.Add("OtraMas");

        Assert.True(CsvHeader.TryParse(Line(columns), out var header, out var error));
        Assert.Null(error);
        Assert.Equal(14, header.FieldCount);
        Assert.Equal(4, header.IndexOf("Ask"));
        Assert.Equal(6, header.IndexOf("IntValue"));   // desplazada por la columna extra
        Assert.Equal(12, header.IndexOf("IV"));
    }

    [Fact]
    [Trait("RF", "RF-01")]
    [Trait("CA", "CA-02")]
    public void Parse_ColumnaExtraRepetida_NoEsError()
    {
        // Solo importan las 12 columnas esperadas; una columna extra repetida no afecta a la lectura.
        var columns = Standard.Concat(["Extra", "extra"]);

        Assert.True(CsvHeader.TryParse(Line(columns), out var header, out var error));
        Assert.Null(error);
        Assert.Equal(14, header.FieldCount);
    }

    [Fact]
    [Trait("RF", "RF-01")]
    [Trait("CA", "CA-02")]
    public void Parse_FaltaAsk_MissingColumnsLinea1()
    {
        var columns = Standard.Where(c => c != "Ask");

        Assert.False(CsvHeader.TryParse(Line(columns), out var header, out var error));
        Assert.Null(header);
        Assert.Equal(CsvErrorKind.MissingColumns, error.Kind);
        Assert.Equal(1, error.Line);
        Assert.Equal(["Ask"], error.MissingColumns);
        Assert.Contains("Ask", error.Message);
    }

    [Fact]
    [Trait("RF", "RF-01")]
    [Trait("CA", "CA-02")]
    public void Parse_FaltanVariasColumnas_ListaTodasEnElOrdenEstandar()
    {
        var columns = Standard.Where(c => c is not ("Bid" or "IV" or "Time"));

        Assert.False(CsvHeader.TryParse(Line(columns), out _, out var error));
        Assert.Equal(CsvErrorKind.MissingColumns, error.Kind);
        Assert.Equal(["Time", "Bid", "IV"], error.MissingColumns);
    }

    [Fact]
    [Trait("RF", "RF-01")]
    [Trait("CA", "CA-02")]
    public void Parse_CabeceraVacia_FaltanLas12()
    {
        Assert.False(CsvHeader.TryParse("", out _, out var error));
        Assert.Equal(CsvErrorKind.MissingColumns, error.Kind);
        Assert.Equal(1, error.Line);
        Assert.Equal(Standard, error.MissingColumns);
    }

    [Theory]
    [InlineData("Time,Time,SpotPrice,StrikePrice,Bid,Ask,IntValue,ExtValue,Delta,Gamma,Theta,Vega,IV", "Time")]
    [InlineData("Time,SpotPrice,StrikePrice,Bid,Ask,IntValue,ExtValue,Delta,Gamma,Theta,Vega,IV,ask", "Ask")]
    [Trait("RF", "RF-01")]
    [Trait("CA", "CA-02")]
    public void Parse_ColumnaRepetida_DuplicateColumn(string line, string expectedColumn)
    {
        Assert.False(CsvHeader.TryParse(line, out var header, out var error));
        Assert.Null(header);
        Assert.Equal(CsvErrorKind.DuplicateColumn, error.Kind);
        Assert.Equal(1, error.Line);
        Assert.Equal(expectedColumn, error.Column);
    }
}
