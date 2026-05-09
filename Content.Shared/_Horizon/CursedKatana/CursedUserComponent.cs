using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Horizon.CursedKatana;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CursedUserComponent : Component
{
    [DataField("ownerUid")]
    public EntityUid OwnerUid;

    [DataField("katanaUid")]
    public EntityUid? KatanaUid;

    [DataField("maskUid")]
    public EntityUid? MaskUid;

    [DataField("demonMaskPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string DemonMaskPrototype = "DemonMask";

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("getDemonMaskAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string GetDemonMaskAction = "ActionGetDemonMask";

    [DataField, AutoNetworkedField]
    public EntityUid? GetDemonMaskActionEntity;
}
