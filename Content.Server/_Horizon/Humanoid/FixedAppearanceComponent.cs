using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Server._Horizon.Humanoid;

[RegisterComponent]
public sealed partial class FixedAppearanceComponent : Component
{
    [DataField]
    public string? HairStyleId;

    [DataField]
    public Color? HairColor;

    [DataField]
    public string? FacialHairStyleId;

    [DataField]
    public Color? FacialHairColor;

    [DataField]
    public Color? EyeColor;

    [DataField]
    public Color? SkinColor;

    [DataField]
    public Sex? Sex;

    [DataField]
    public Gender? Gender;

    [DataField]
    public int? Age;

    [DataField]
    public string? Species;

    [DataField]
    public Dictionary<MarkingCategories, List<FixedMarking>>? Markings;
}
[DataDefinition]
public sealed partial class FixedMarking
{
    [DataField(required: true)]
    public string MarkingId { get; private set; } = default!;

    [DataField]
    public List<Color> Colors { get; private set; } = new();
}
