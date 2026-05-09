using Content.Shared._Horizon.OutpostCapture;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.FlavorText;

[Prototype]
public sealed partial class CharacterFactionPrototype : IPrototype
{
    [IdDataField]
    [ViewVariables]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = default!;

    [DataField(required: true)]
    public string Desc = default!;

    [DataField]
    public Color Color = Color.White;

    [DataField]
    public ProtoId<OutpostSpawnPrototype>? OutpostSpawnListProto = "default";

    [DataField]
    public ProtoId<RadioChannelPrototype>? RadioChannel = "Common"; // Test only

    [DataField]
    public bool Roundstart = true;
}
