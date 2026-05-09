using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Bark;

[RegisterComponent, NetworkedComponent]
public sealed partial class SpeechBarksComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public BarkData Data = new();
}

