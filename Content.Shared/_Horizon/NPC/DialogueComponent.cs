using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility; // Добавить для SpriteSpecifier

namespace Content.Shared._Horizon.NPC
{
    [RegisterComponent]
    public sealed partial class DialogueComponent : Component
    {
        [DataField("dialogueTree", customTypeSerializer: typeof(PrototypeIdSerializer<DialogueTreePrototype>))]
        public string DialogueTree = "DefaultDialogue";

        [DataField("canFollowAfterDialogue")]
        public bool CanFollowAfterDialogue = true;

        [ViewVariables]
        public EntityUid? ConversationPartner;

        [ViewVariables]
        public bool IsInConversation = false;
    }
}
