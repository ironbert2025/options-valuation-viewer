using OptionsViewer.Core;

namespace OptionsViewer.Tests;

/// <summary>Huecos de la serie con los CSV reales de samples/ (CA-05, RNF-10).</summary>
public class ReferenceSeriesGapsTests
{
    private static ContractSeries BuildOk(decimal strike, TimeOnly t0)
    {
        var result = ValuationSeries.Build(ReferenceData.CallsSession, new ContractKey(OptionType.Call, strike), t0);
        Assert.True(result.IsSuccess, result.Error is null ? null : $"{result.Error.Kind}");
        return result.Value!;
    }

    [Fact]
    [Trait("RF", "RF-06")]
    [Trait("CA", "CA-05")]
    public void Build_Calls337_5_T0_094449_NueveHuecosYVuelveA094529()
    {
        var t0 = new TimeOnly(9, 44, 49);
        var back = new TimeOnly(9, 45, 29);

        var series = BuildOk(337.5m, t0);

        // Los snapshots de la sesión estrictamente entre 09:44:49 y 09:45:29, según los datos reales
        var sessionTimesBetween = ReferenceData.CallsSession.Snapshots
            .Select(s => s.Time).Where(t => t > t0 && t < back).ToList();
        Assert.Equal(9, sessionTimesBetween.Count);
        Assert.Equal(
            [new TimeOnly(9, 44, 53), new TimeOnly(9, 44, 57), new TimeOnly(9, 45, 1), new TimeOnly(9, 45, 5), new TimeOnly(9, 45, 9),
             new TimeOnly(9, 45, 13), new TimeOnly(9, 45, 17), new TimeOnly(9, 45, 21), new TimeOnly(9, 45, 25)],
            sessionTimesBetween);

        // Por cada snapshot de la sesión desde T0 hay un punto, también donde el contrato falta
        var upToBack = series.Points.TakeWhile(p => p.Time <= back).ToList();
        Assert.Equal(11, upToBack.Count);                       // T0, 9 huecos y el regreso

        Assert.Equal(t0, upToBack[0].Time);
        Assert.NotNull(upToBack[0].Value);                      // 09:44:49 tiene datos

        var gaps = upToBack.Skip(1).Take(9).ToList();           // exactamente 9 puntos con Value == null
        Assert.All(gaps, p => Assert.Null(p.Value));
        Assert.Equal(sessionTimesBetween, gaps.Select(p => p.Time));   // y son los snapshots de la sesión entre las dos horas

        Assert.Equal(back, upToBack[10].Time);
        Assert.NotNull(upToBack[10].Value);                     // 09:45:29 tiene datos

        // No se interpola: los valores a ambos lados del hueco son los de sus propios snapshots
        var before = ReferenceData.CallsSession.Snapshots.Single(s => s.Time == t0).QuotesByStrike[337.5m];
        var after = ReferenceData.CallsSession.Snapshots.Single(s => s.Time == back).QuotesByStrike[337.5m];
        Assert.Equal(before.Bid * 100m, upToBack[0].Value!.BidPerContract);
        Assert.Equal(after.Bid * 100m, upToBack[10].Value!.BidPerContract);
        Assert.Equal(after.Ask * 100m, upToBack[10].Value!.AskPerContract);
    }

    [Fact]
    [Trait("RF", "RF-06")]
    [Trait("CA", "CA-05")]
    public void Build_Calls337_5_TieneUnPuntoPorCadaSnapshotDeLaSesionDesdeT0()
    {
        var t0 = new TimeOnly(9, 44, 49);

        var series = BuildOk(337.5m, t0);

        var snapshotTimes = ReferenceData.CallsSession.Snapshots.Select(s => s.Time).Where(t => t >= t0).ToList();
        Assert.Equal(snapshotTimes, series.Points.Select(p => p.Time));   // ni se omiten ni se inventan puntos

        // Cada punto con Value null corresponde a un snapshot donde el strike realmente no está, y viceversa
        foreach (var point in series.Points)
        {
            var snapshot = ReferenceData.CallsSession.Snapshots.Single(s => s.Time == point.Time);
            Assert.Equal(snapshot.QuotesByStrike.ContainsKey(337.5m), point.Value is not null);
        }
    }

    [Fact]
    [Trait("RF", "RF-06")]
    [Trait("CA", "CA-05")]
    [Trait("RNF", "RNF-10")]
    public void Build_Calls342_5_SinHuecosAunqueLosSnapshotsSeSeparenHasta181Segundos()
    {
        // El 342,5 está en todos los snapshots. Después de las 11:00 hay saltos de 60 s y de hasta 181 s entre
        // snapshots (spec): el tiempo transcurrido nunca corta la línea.
        var series = BuildOk(342.5m, new TimeOnly(9, 30, 0));

        var maxGapSeconds = series.Points.Zip(series.Points.Skip(1), (a, b) => (b.Time - a.Time).TotalSeconds).Max();
        Assert.Equal(181, maxGapSeconds);                       // premisa: los datos sí tienen ese salto
        Assert.All(series.Points, p => Assert.NotNull(p.Value));
    }
}
