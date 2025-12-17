using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.FlavorText;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CharacterFactionMemberComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<CharacterFactionPrototype> Faction = "None";
}
