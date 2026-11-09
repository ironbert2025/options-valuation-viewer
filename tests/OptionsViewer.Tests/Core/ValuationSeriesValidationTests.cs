using OptionsViewer.Core;

namespace OptionsViewer.Tests;

public class ValuationSeriesValidationTests
{
    private static readonly ContractKey Call340 = new(OptionType.Call, 340.0m);

    private static Snapshot Snap(string time, params decimal[] strikes) =>
        new(TimeOnly.ParseExact(time, "HH:mm:ss"),
            strikes.ToDictionary(
                s => s,
                s => new ContractQuote(s, 341.5m, 2.89m, 3.0m, 1.5m, 1.4m, 0.6m, 0.1m, -0.3m, 0.03m, 42.5m)));

    private static TradingSession SessionOf(OptionType type, params Snapshot[] snapshots) =>
        new(type, snapshots, DiscardedCount: 0, NoSnapshotsInWindow: snapshots.Length == 0);

    private static void AssertError(SeriesResult result, SeriesErrorKind kind, TimeOnly t0, ContractKey key)
    {
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.NotNull(result.Error);
        Assert.Equal(kind, result.Error.Kind);
        Assert.Equal(t0, result.Error.T0);
        Assert.Equal(key, result.Error.Key);
    }

    [Fact]
    [Trait("RF", "RF-04")]
    public void Build_SesionVacia_EmptySession()
    {
        var session = SessionOf(OptionType.Call);
        var t0 = new TimeOnly(9, 30, 0);

        var result = ValuationSeries.Build(session, Call340, t0);

        AssertError(result, SeriesErrorKind.EmptySession, t0, Call340);
    }

    [Fact]
    [Trait("RF", "RF-04")]
    public void Build_T0QueNoEsSnapshot_T0NotASnapshot()
    {
        var session = SessionOf(OptionType.Call, Snap("09:30:00", 340m), Snap("09:30:04", 340m));
        var t0 = new TimeOnly(9, 30, 2);   // entre dos snapshots: no coincide con ninguno

        var result = ValuationSeries.Build(session, Call340, t0);

        AssertError(result, SeriesErrorKind.T0NotASnapshot, t0, Call340);
    }

    [Fact]
    [Trait("RF", "RF-04")]
    [Trait("CA", "CA-06")]
    public void Build_ContratoAusenteEnT0_ContractAbsentAtT0()
    {
        // El snapshot de las 09:30:04 existe pero no trae el strike 340 (sintético)
        var session = SessionOf(OptionType.Call, Snap("09:30:00", 340m, 342.5m), Snap("09:30:04", 342.5m));
        var t0 = new TimeOnly(9, 30, 4);

        var result = ValuationSeries.Build(session, Call340, t0);

        AssertError(result, SeriesErrorKind.ContractAbsentAtT0, t0, Call340);
    }

    [Fact]
    [Trait("RF", "RF-04")]
    public void Build_TipoDistinto_LanzaArgumentException()
    {
        var session = SessionOf(OptionType.Call, Snap("09:30:00", 340m));
        var putKey = new ContractKey(OptionType.Put, 340.0m);

        // Es un error de programación, no un resultado de usuario
        var ex = Assert.Throws<ArgumentException>(() => ValuationSeries.Build(session, putKey, new TimeOnly(9, 30, 0)));
        Assert.Equal("key", ex.ParamName);
    }

    [Fact]
    [Trait("RF", "RF-04")]
    public void Build_TipoDistintoConSesionVacia_LanzaArgumentExceptionEnVezDeEmptySession()
    {
        var session = SessionOf(OptionType.Call);
        var putKey = new ContractKey(OptionType.Put, 340.0m);

        Assert.Throws<ArgumentException>(() => ValuationSeries.Build(session, putKey, new TimeOnly(9, 30, 0)));
    }
}
