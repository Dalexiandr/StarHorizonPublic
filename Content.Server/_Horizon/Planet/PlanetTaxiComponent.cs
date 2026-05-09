namespace Content.Server._Horizon.Planet;

[RegisterComponent]
public sealed partial class PlanetTaxiComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextLaunch = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan StopDuration = TimeSpan.FromSeconds(60);

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan FTLTime = TimeSpan.FromSeconds(25);

    [ViewVariables(VVAccess.ReadWrite)]
    public int CurIdx = 0;
}
