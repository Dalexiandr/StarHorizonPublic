using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Traits;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LowPainToleranceComponent : Component
{
    [DataField, AutoNetworkedField]
    public float DamageModifier = 0.15f;
}
