using OptionsViewer.Core;

namespace OptionsViewer.Tests;

public class SeriesPointTests
{
    [Theory]
    [InlineData(9, 30, 0, 34_200)]     // apertura
    [InlineData(16, 0, 0, 57_600)]     // cierre
    [InlineData(0, 0, 0, 0)]
    [InlineData(23, 59, 59, 86_399)]
    [Trait("RF", "RF-06")]
    public void SeriesPoint_SecondsOfDay_ConvierteLaHoraEnSegundosDesdeMedianoche(int h, int m, int s, int expected)
    {
        var point = new SeriesPoint(new TimeOnly(h, m, s), null);

        Assert.Equal(expected, point.SecondsOfDay);
    }

    [Fact]
    [Trait("RF", "RF-06")]
    public void SeriesPoint_SecondsOfDay_SeCalculaDeTimeYNoPuedeDesincronizarse()
    {
        // SecondsOfDay no es un dato que se pase al constructor: se deriva siempre de Time, también al copiar con "with"
        var point = new SeriesPoint(new TimeOnly(9, 30, 0), null);
        var moved = point with { Time = new TimeOnly(10, 0, 0) };

        Assert.Equal(34_200, point.SecondsOfDay);
        Assert.Equal(36_000, moved.SecondsOfDay);
    }

    [Fact]
    [Trait("RF", "RF-06")]
    public void SeriesPoint_SinValor_EsUnHueco()
    {
        var gap = new SeriesPoint(new TimeOnly(9, 44, 53), null);
        var withData = new SeriesPoint(new TimeOnly(9, 44, 49), new PricePoint(300m, 289m, -11m, -3.6667m));

        Assert.Null(gap.Value);
        Assert.NotNull(withData.Value);
    }

    [Fact]
    public void SeriesResult_IsSuccess_DependeDelError()
    {
        var key = new ContractKey(OptionType.Call, 340m);
        var error = new SeriesError(SeriesErrorKind.ContractAbsentAtT0, new TimeOnly(9, 44, 53), key);
        var series = new ContractSeries(key, new TimeOnly(9, 30, 0), 300m, true, []);

        Assert.True(new SeriesResult(series, null).IsSuccess);
        Assert.False(new SeriesResult(null, error).IsSuccess);
    }
}
