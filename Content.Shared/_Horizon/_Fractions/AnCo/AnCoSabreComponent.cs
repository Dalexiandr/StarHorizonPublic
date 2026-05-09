using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon._Fractions.AnCo;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AnCoSabreComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? BoundOwner;

    [DataField]
    public EntProtoId RecallAction = "ActionAnCoSabreRecall";

    [DataField, AutoNetworkedField]
    public EntityUid? RecallActionEntity;
}
