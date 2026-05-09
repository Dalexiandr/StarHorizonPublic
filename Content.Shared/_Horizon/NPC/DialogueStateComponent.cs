// DialogueStateComponent.cs
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.NPC
{
    [RegisterComponent]
    public sealed partial class DialogueStateComponent : Component
    {
        [DataField("currentState")]
        public DialogueState State = DialogueState.Idle;

        [DataField("currentResponse")]
        public string? CurrentResponse;
    }

    public enum DialogueState : byte
    {
        Idle,
        Talking,
        Following,
        WaitingResponse
    }
}
