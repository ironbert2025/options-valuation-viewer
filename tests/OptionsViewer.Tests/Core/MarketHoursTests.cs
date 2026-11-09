using OptionsViewer.Core;

namespace OptionsViewer.Tests;

public class MarketHoursTests
{
    // CA-09: la sesión es 09:30:00 a 16:00:00, ambos extremos incluidos, sobre la hora tal como viene (sin zonas)

    [Fact]
    [Trait("RF", "RF-02")]
    [Trait("CA", "CA-09")]
    public void IsInSession_09_29_59_Excluye()
    {
        Assert.False(MarketHours.IsInSession(new TimeOnly(9, 29, 59)));
    }

    [Fact]
    [Trait("RF", "RF-02")]
    [Trait("CA", "CA-09")]
    public void IsInSession_09_30_00_Incluye()
    {
        Assert.True(MarketHours.IsInSession(new TimeOnly(9, 30, 0)));
    }

    [Fact]
    [Trait("RF", "RF-02")]
    [Trait("CA", "CA-09")]
    public void IsInSession_16_00_00_Incluye()
    {
        Assert.True(MarketHours.IsInSession(new TimeOnly(16, 0, 0)));
    }

    [Fact]
    [Trait("RF", "RF-02")]
    [Trait("CA", "CA-09")]
    public void IsInSession_16_00_01_Excluye()
    {
        Assert.False(MarketHours.IsInSession(new TimeOnly(16, 0, 1)));
    }
}
