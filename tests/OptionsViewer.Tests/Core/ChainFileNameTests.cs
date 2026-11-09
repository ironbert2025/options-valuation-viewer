using OptionsViewer.Core;

namespace OptionsViewer.Tests;

public class ChainFileNameTests
{
    // Nombres reales de los archivos de samples/
    private const string CallsSample = "AAPL_Call_20261104_20261104.csv";
    private const string PutsSample = "AAPL_Put_20261104_20261104.csv";

    [Fact]
    public void TryGetOptionType_NombreConIdYCall_DevuelveCall()
    {
        // El patrón de la spec admite un <id> inicial: <id>_AAPL_<Call|Put>_<fecha>_<fecha>.csv
        Assert.Equal(OptionType.Call, ChainFileName.TryGetOptionType("42_" + CallsSample));
    }

    [Fact]
    public void TryGetOptionType_SinIdYPut_DevuelvePut()
    {
        Assert.Equal(OptionType.Put, ChainFileName.TryGetOptionType(PutsSample));
    }

    [Fact]
    public void TryGetOptionType_SinIdYCall_DevuelveCall()
    {
        Assert.Equal(OptionType.Call, ChainFileName.TryGetOptionType(CallsSample));
    }

    [Theory]
    [InlineData(CallsSample, OptionType.Call)]
    [InlineData(PutsSample, OptionType.Put)]
    public void TryGetOptionType_Minusculas_Reconoce(string sampleName, OptionType expected)
    {
        Assert.Equal(expected, ChainFileName.TryGetOptionType(sampleName.ToLowerInvariant()));
    }

    [Theory]
    [InlineData("AAPL_Straddle_20261104_20261104.csv")]
    [InlineData("AAPL_20261104_20261104.csv")]
    [InlineData("AAPLCall20261104.csv")]
    [InlineData("")]
    public void TryGetOptionType_SinTipo_DevuelveNull(string fileName)
    {
        Assert.Null(ChainFileName.TryGetOptionType(fileName));
    }
}
