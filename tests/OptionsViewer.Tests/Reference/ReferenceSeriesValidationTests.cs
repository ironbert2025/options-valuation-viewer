using OptionsViewer.Core;

namespace OptionsViewer.Tests;

/// <summary>Errores de entrada de la serie con los CSV reales de samples/ (CA-06).</summary>
public class ReferenceSeriesValidationTests
{
    [Fact]
    [Trait("RF", "RF-04")]
    [Trait("CA", "CA-06")]
    public void Build_Calls337_5_T0_094453_ContractAbsentAtT0()
    {
        var session = ReferenceData.CallsSession;
        var key = new ContractKey(OptionType.Call, 337.5m);
        var t0 = new TimeOnly(9, 44, 53);

        // Premisa de la prueba, comprobada con los datos reales: ese snapshot existe en la sesión pero no trae el strike 337,5
        var snapshot = Assert.Single(session.Snapshots, s => s.Time == t0);
        Assert.False(snapshot.QuotesByStrike.ContainsKey(337.5m));
        Assert.NotEmpty(snapshot.QuotesByStrike);

        var result = ValuationSeries.Build(session, key, t0);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal(SeriesErrorKind.ContractAbsentAtT0, result.Error!.Kind);
        Assert.Equal(t0, result.Error.T0);
        Assert.Equal(key, result.Error.Key);
    }

    [Fact]
    [Trait("RF", "RF-04")]
    public void Build_T0DescartadoPorHorario_T0NotASnapshot()
    {
        // 08:24:27 es un snapshot real del archivo, pero cae fuera de horario y no está en la sesión
        var key = new ContractKey(OptionType.Call, 340.0m);
        var t0 = new TimeOnly(8, 24, 27);

        var result = ValuationSeries.Build(ReferenceData.CallsSession, key, t0);

        Assert.Null(result.Value);
        Assert.Equal(SeriesErrorKind.T0NotASnapshot, result.Error!.Kind);
    }

    [Fact]
    [Trait("RF", "RF-04")]
    public void Build_PutsConClaveCall_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            ValuationSeries.Build(ReferenceData.PutsSession, new ContractKey(OptionType.Call, 340.0m), new TimeOnly(9, 30, 0)));
    }
}
