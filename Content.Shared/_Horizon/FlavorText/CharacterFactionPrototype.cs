using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.FlavorText;

[Prototype]
public sealed partial class CharacterFactionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = default!;

    [DataField(required: true)]
    public string Desc = default!;

    [DataField]
    public Color Color = Color.White;

    [DataField]
    public bool Roundstart = true;
}
