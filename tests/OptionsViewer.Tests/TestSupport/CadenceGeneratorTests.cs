using OptionsViewer.Core;
using OptionsViewer.Infrastructure;

namespace OptionsViewer.Tests;

public class CadenceGeneratorTests
{
    private static int Sec(string time) => (int)TimeOnly.ParseExact(time, "HH:mm:ss").ToTimeSpan().TotalSeconds;

    // Nº de snapshots de un tramo, calculado con aritmética (no escrito a mano): (hasta − desde) / paso + 1
    private static int ExpectedCount(string from, string to, int step) => (Sec(to) - Sec(from)) / step + 1;

    [Fact]
    [Trait("RNF", "RNF-10")]
    [Trait("RNF", "RNF-08")]
    public void Generador_6sHasta11_1minDespues_ProduceLosSnapshotsEsperados()
    {
        CadenceSegment[] segments =
        [
            CadenceSegment.Of("08:30:00", "09:29:54", 6),     // antes de la apertura
            CadenceSegment.Of("09:30:00", "10:59:54", 6),     // 6 s hasta las 11:00
            CadenceSegment.Of("11:00:00", "16:00:00", 60),    // 1 minuto desde las 11:00 hasta el cierre
            CadenceSegment.Of("16:01:00", "17:00:00", 60),    // después del cierre
        ];

        var times = CadenceSegment.AllTimes(segments);

        // Conteo por tramo, calculado y no escrito a mano
        var expectedTotal = segments.Sum(s => ExpectedCount(s.From.ToString("HH:mm:ss"), s.To.ToString("HH:mm:ss"), s.StepSeconds));
        Assert.Equal(expectedTotal, times.Count);

        // Estrictamente creciente, con la cadencia de cada tramo y con horas fuera de horario a ambos lados
        Assert.Equal(times.OrderBy(t => t).ToList(), times);
        Assert.Equal(times.Count, times.Distinct().Count());
        Assert.Contains(new TimeOnly(10, 59, 54), times);
        Assert.Contains(new TimeOnly(11, 0, 0), times);
        Assert.Equal(6, (times[times.IndexOf(new TimeOnly(10, 59, 54))] - times[times.IndexOf(new TimeOnly(10, 59, 48))]).TotalSeconds);
        Assert.Equal(60, (times[times.IndexOf(new TimeOnly(11, 1, 0))] - times[times.IndexOf(new TimeOnly(11, 0, 0))]).TotalSeconds);
        Assert.Contains(times, t => t < new TimeOnly(9, 30, 0));
        Assert.Contains(times, t => t > new TimeOnly(16, 0, 0));
        Assert.Equal(new TimeOnly(8, 30, 0), times[0]);
        Assert.Equal(new TimeOnly(17, 0, 0), times[^1]);
    }

    [Fact]
    [Trait("RNF", "RNF-10")]
    [Trait("RNF", "RNF-08")]
    public void Generador_CsvGenerado_SeCargaConUnContratoSiempreYOtroConHuecosDefinidos()
    {
        var times = CadenceSegment.AllTimes(
        [
            CadenceSegment.Of("09:30:00", "09:30:30", 6),
            CadenceSegment.Of("09:31:00", "09:33:00", 60),
        ]);
        var withoutSecond = new HashSet<TimeOnly> { new(9, 30, 12), new(9, 30, 18), new(9, 32, 0) };

        using var file = TempFile.WithContent(SyntheticCsv.FromTimes(times, withoutSecond).Build());
        var result = new CsvFileSnapshotSource(file.Path, OptionType.Call).Load(TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess, result.Error?.Message);
        var snapshots = result.Value!.Snapshots;
        Assert.Equal(times, snapshots.Select(s => s.Time));
        Assert.All(snapshots, s => Assert.True(s.QuotesByStrike.ContainsKey(340.0m)));     // el contrato siempre presente
        Assert.Equal(                                                                          // el otro falta justo donde se definió
            withoutSecond.Order(),
            snapshots.Where(s => !s.QuotesByStrike.ContainsKey(342.5m)).Select(s => s.Time));
        Assert.All(snapshots.SelectMany(s => s.QuotesByStrike.Values), q => Assert.True(q.Bid > 0 && q.Bid <= q.Ask));
    }

    [Fact]
    [Trait("RNF", "RNF-10")]
    public void Generador_PasoQueNoDivideElTramo_NoSuperaElLimite()
    {
        // 09:30:00 a 09:30:20 cada 6 s → 00, 06, 12, 18 (el 24 queda fuera)
        var times = CadenceSegment.Of("09:30:00", "09:30:20", 6).Times().ToList();

        Assert.Equal([new TimeOnly(9, 30, 0), new TimeOnly(9, 30, 6), new TimeOnly(9, 30, 12), new TimeOnly(9, 30, 18)], times);
    }
}
