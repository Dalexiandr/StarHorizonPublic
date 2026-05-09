using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Horizon.NPC
{
    [Serializable, NetSerializable]
    [Prototype("dialogueTree")]
    public sealed class DialogueTreePrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; } = default!;

        [DataField("dialogues")]
        public List<DialogueEntry> Dialogues = new();
    }

    [Serializable, NetSerializable]
    [DataDefinition]
    public sealed partial class DialogueEntry
    {
        [DataField("text")]
        public string Text = string.Empty;

        [DataField("responses")]
        public List<DialogueResponse> Responses = new();

        [DataField("nextNode")]
        public string? NextNode;

        [DataField("id", required: false)]
        public string? Id;
    }

    [Serializable, NetSerializable]
    [DataDefinition]
    public sealed partial class DialogueResponse
    {
        [DataField("text")]
        public string Text = string.Empty;

        [DataField("nextNode")]
        public string? NextNode;

        [DataField("action")]
        public string? Action; // Например, "follow", "trade", "attack"
    }
}
