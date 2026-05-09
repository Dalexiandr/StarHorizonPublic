using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Speech;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Horizon.CursedKatana;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DemonMaskComponent : Component
{
    [DataField("ownerUid")]
    public EntityUid OwnerUid;

    [DataField("ownerIdentified")]
    public bool OwnerIdentified { get; set; } = false;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("speechSounds")]
    public ProtoId<SpeechSoundsPrototype>? OriginalSpeechSounds;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("recallCursedKatanaAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string RecallCursedKatanaAction = "ActionRecallCursedKatana";

    [DataField, AutoNetworkedField]
    public EntityUid? RecallCursedKatanaActionEntity;
}
