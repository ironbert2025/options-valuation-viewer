namespace OptionsViewer.Tests;

public class TestSupportTests
{
    [Fact]
    public void SamplePaths_ArchivosDeReferenciaExisten()
    {
        Assert.True(File.Exists(SamplePaths.Calls), $"No existe {SamplePaths.Calls}");
        Assert.True(File.Exists(SamplePaths.Puts), $"No existe {SamplePaths.Puts}");
        Assert.True(new FileInfo(SamplePaths.Calls).Length > 0);
        Assert.True(new FileInfo(SamplePaths.Puts).Length > 0);
    }

    [Fact]
    public void SamplePaths_Cabecera_CoincideConLasColumnasEstandar()
    {
        var expected = string.Join(",", SyntheticCsv.StandardColumns);
        Assert.Equal(expected, File.ReadLines(SamplePaths.Calls).First());
        Assert.Equal(expected, File.ReadLines(SamplePaths.Puts).First());
    }

    [Fact]
    public void SyntheticCsv_Construye_CabeceraYFilasConCrlf()
    {
        var csv = new SyntheticCsv()
            .AddRow("09:30:00", "341.53", "340.0", "2.89", "3.0", "-1.53", "4.53", "0.5", "0.07", "-1.1", "0.04", "50.0")
            .AddRow("09:30:04", "342.21", "340.0", "3.05", "3.35", "-2.21", "5.56", "0.5", "0.07", "-1.1", "0.04", "50.1")
            .Build();

        Assert.Equal(
            "Time,SpotPrice,StrikePrice,Bid,Ask,IntValue,ExtValue,Delta,Gamma,Theta,Vega,IV\r\n"
            + "09:30:00,341.53,340.0,2.89,3.0,-1.53,4.53,0.5,0.07,-1.1,0.04,50.0\r\n"
            + "09:30:04,342.21,340.0,3.05,3.35,-2.21,5.56,0.5,0.07,-1.1,0.04,50.1\r\n",
            csv);
    }

    [Fact]
    public void SyntheticCsv_ConLf_NoContieneRetornoDeCarro()
    {
        var csv = new SyntheticCsv().WithLineEnding("\n").AddRow("a", "b").Build();

        Assert.DoesNotContain('\r', csv);
        Assert.Equal(2, csv.Count(c => c == '\n'));
    }

    [Fact]
    public void SyntheticCsv_ConColumnasReordenadasYExtra_UsaLaCabeceraIndicada()
    {
        var csv = new SyntheticCsv()
            .WithColumns("ask", "Time", "Extra")
            .AddRow("3.0", "09:30:00", "x")
            .Build();

        Assert.Equal("ask,Time,Extra\r\n3.0,09:30:00,x\r\n", csv);
    }
}
