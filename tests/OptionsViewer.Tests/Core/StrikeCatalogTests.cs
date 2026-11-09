using OptionsViewer.Core;

namespace OptionsViewer.Tests;

public class StrikeCatalogTests
{
    private static Snapshot Snap(string time, params decimal[] strikes) =>
        new(TimeOnly.ParseExact(time, "HH:mm:ss"),
            strikes.ToDictionary(
                s => s,
                s => new ContractQuote(s, 341.5m, 2.89m, 3.0m, 1.5m, 1.4m, 0.6m, 0.1m, -0.3m, 0.03m, 42.5m)));

    [Fact]
    [Trait("RF", "RF-03")]
    public void List_SesionVacia_ListaVacia()
    {
        var session = new TradingSession(OptionType.Call, [], DiscardedCount: 0, NoSnapshotsInWindow: true);

        Assert.Empty(StrikeCatalog.List(session));
    }

    [Fact]
    [Trait("RF", "RF-03")]
    public void List_StrikesDesordenadosYRepetidosEntreSnapshots_DistintosYOrdenados()
    {
        var session = new TradingSession(
            OptionType.Call,
            [
                Snap("09:30:00", 342.5m, 337.5m, 340m),
                Snap("09:30:04", 345m, 340m, 342.5m),
                Snap("09:31:04", 335m, 337.5m),
            ],
            DiscardedCount: 0,
            NoSnapshotsInWindow: false);

        Assert.Equal([335m, 337.5m, 340m, 342.5m, 345m], StrikeCatalog.List(session));
    }
}
