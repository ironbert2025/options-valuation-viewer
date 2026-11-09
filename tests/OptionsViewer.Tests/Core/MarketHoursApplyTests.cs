using OptionsViewer.Core;

namespace OptionsViewer.Tests;

public class MarketHoursApplyTests
{
    private static Snapshot Snap(TimeOnly time) =>
        new(time, new Dictionary<decimal, ContractQuote>
        {
            [340.0m] = new ContractQuote(340.0m, 341.5m, 2.89m, 3.0m, 1.5m, 1.4m, 0.6m, 0.1m, -0.3m, 0.03m, 42.5m),
        });

    private static Snapshot Snap(string time) => Snap(TimeOnly.ParseExact(time, "HH:mm:ss"));

    private static SnapshotSet SetOf(OptionType type, params string[] times) =>
        new(type, times.Select(Snap).ToList());

    [Fact]
    [Trait("RF", "RF-02")]
    public void Apply_MezclaDentroYFuera_ConservaYCuentaDescartados()
    {
        var set = SetOf(OptionType.Call,
            "08:24:27", "09:29:59", "09:30:00", "12:00:00", "16:00:00", "16:00:01", "18:24:35");

        var session = MarketHours.Apply(set);

        Assert.Equal(OptionType.Call, session.Type);
        Assert.Equal(
            [new TimeOnly(9, 30, 0), new TimeOnly(12, 0, 0), new TimeOnly(16, 0, 0)],
            session.Snapshots.Select(s => s.Time));
        Assert.Equal(4, session.DiscardedCount);
        Assert.False(session.NoSnapshotsInWindow);
    }

    [Fact]
    [Trait("RF", "RF-10")]
    [Trait("CA", "CA-12")]
    public void Apply_TodosFuera_NoSnapshotsInWindow()
    {
        // CA-12: un archivo con horas de 08:00 a 08:59 (p. ej. grabado en otra zona horaria) no tiene ninguna dentro
        var times = Enumerable.Range(0, 60).Select(m => new TimeOnly(8, m, 0).ToString("HH:mm:ss")).ToArray();
        var set = SetOf(OptionType.Put, times);

        var session = MarketHours.Apply(set);

        Assert.Empty(session.Snapshots);
        Assert.Equal(60, session.DiscardedCount);
        Assert.True(session.NoSnapshotsInWindow);
        Assert.Equal(OptionType.Put, session.Type);
    }

    [Fact]
    [Trait("RF", "RF-10")]
    public void Apply_SetSinSnapshots_NoSnapshotsInWindowYSinDescartados()
    {
        var session = MarketHours.Apply(new SnapshotSet(OptionType.Call, []));

        Assert.Empty(session.Snapshots);
        Assert.Equal(0, session.DiscardedCount);
        Assert.True(session.NoSnapshotsInWindow);
    }

    [Fact]
    [Trait("RF", "RF-02")]
    [Trait("CA", "CA-09")]
    public void Apply_SnapshotEn1600_Conserva()
    {
        var session = MarketHours.Apply(SetOf(OptionType.Call, "15:59:06", "16:00:00", "16:00:01"));

        Assert.Equal([new TimeOnly(15, 59, 6), new TimeOnly(16, 0, 0)], session.Snapshots.Select(s => s.Time));
        Assert.Equal(1, session.DiscardedCount);
    }

    [Fact]
    [Trait("RF", "RF-02")]
    public void Apply_ConservaElOrdenYLasMismasInstancias()
    {
        var set = SetOf(OptionType.Call, "09:30:00", "09:30:04", "09:31:04", "09:34:05");

        var session = MarketHours.Apply(set);

        Assert.Equal(set.Snapshots.Count, session.Snapshots.Count);
        for (int i = 0; i < set.Snapshots.Count; i++)
            Assert.Same(set.Snapshots[i], session.Snapshots[i]);
        Assert.Equal(0, session.DiscardedCount);
        Assert.False(session.NoSnapshotsInWindow);
    }
}
