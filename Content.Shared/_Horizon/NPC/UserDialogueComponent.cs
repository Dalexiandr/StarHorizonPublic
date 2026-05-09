using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.NPC
{
    [RegisterComponent]
    public sealed partial class UserDialogueComponent : Component
    {
        [ViewVariables]
        public EntityUid? CurrentNpc;

        [ViewVariables]
        public DialogueEntry? CurrentDialogue;
    }
}
