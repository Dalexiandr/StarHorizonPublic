using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
namespace Content.Shared._Horizon.Medical.Limbs;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LimbWithItemsComponent : Component, IWithAction
{
    [DataField(readOnly: true, required: true), AutoNetworkedField]
    public List<EntProtoId> Items;

    [DataField, AutoNetworkedField]
    public List<EntityUid> ItemEntities = [];

    [DataField, AutoNetworkedField]
    public bool EntityIcon { get; set; }

    [DataField, AutoNetworkedField]
    public EntProtoId Action { get; set; } = "ActionToggleCyberLimb";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity { get; set; }

    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound;

    [DataField, AutoNetworkedField]
    public bool Toggled;
}
