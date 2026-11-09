using OptionsViewer.Core;

namespace OptionsViewer.Tests;

/// <summary>CA-03 con los CSV reales de samples/: la lista completa de strikes de la sesión, ordenada.</summary>
public class ReferenceStrikesTests
{
    // 16 strikes dentro de horario. Calls: 330 a 367,5. Puts: 327,5 a 365. Pasos de 2,5.
    private static readonly decimal[] CallStrikes =
        [330m, 332.5m, 335m, 337.5m, 340m, 342.5m, 345m, 347.5m, 350m, 352.5m, 355m, 357.5m, 360m, 362.5m, 365m, 367.5m];

    private static readonly decimal[] PutStrikes =
        [327.5m, 330m, 332.5m, 335m, 337.5m, 340m, 342.5m, 345m, 347.5m, 350m, 352.5m, 355m, 357.5m, 360m, 362.5m, 365m];

    [Fact]
    [Trait("RF", "RF-03")]
    [Trait("CA", "CA-03")]
    public void List_CallsReferencia_16StrikesDe330A367_5()
    {
        var strikes = StrikeCatalog.List(ReferenceData.CallsSession);

        Assert.Equal(16, strikes.Count);
        Assert.Equal(CallStrikes, strikes);   // la lista completa, en este orden
    }

    [Fact]
    [Trait("RF", "RF-03")]
    [Trait("CA", "CA-03")]
    public void List_PutsReferencia_16StrikesDe327_5A365()
    {
        var strikes = StrikeCatalog.List(ReferenceData.PutsSession);

        Assert.Equal(16, strikes.Count);
        Assert.Equal(PutStrikes, strikes);
    }
}
