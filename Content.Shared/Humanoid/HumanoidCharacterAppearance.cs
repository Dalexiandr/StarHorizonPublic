using System.Linq;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class HumanoidCharacterAppearance : ICharacterAppearance, IEquatable<HumanoidCharacterAppearance>
{
    [DataField("hair")]
    public string HairStyleId { get; set; } = HairStyles.DefaultHairStyle;

    [DataField]
    public Color HairColor { get; set; } = Color.Black;

    [DataField("facialHair")]
    public string FacialHairStyleId { get; set; } = HairStyles.DefaultFacialHairStyle;

    [DataField]
    public Color FacialHairColor { get; set; } = Color.Black;

    [DataField]
    public Color EyeColor { get; set; } = Color.Black;

    [DataField]
    public Color SkinColor { get; set; } = Humanoid.SkinColor.ValidHumanSkinTone;

    [DataField]
    public List<Marking> Markings { get; set; } = new();

    // _Horizon: Hair gradient
    [DataField]
    public bool HairGradientEnabled { get; set; }

    [DataField]
    public Color HairGradientSecondaryColor { get; set; } = Color.White;

    [DataField]
    public int HairGradientDirection { get; set; }

    [DataField]
    public bool FacialHairGradientEnabled { get; set; }

    [DataField]
    public Color FacialHairGradientSecondaryColor { get; set; } = Color.White;

    [DataField]
    public int FacialHairGradientDirection { get; set; }

    [DataField]
    public bool AllMarkingsGradientEnabled { get; set; }

    [DataField]
    public Color AllMarkingsGradientSecondaryColor { get; set; } = Color.White;

    [DataField]
    public int AllMarkingsGradientDirection { get; set; }

    public HumanoidCharacterAppearance(string hairStyleId,
        Color hairColor,
        string facialHairStyleId,
        Color facialHairColor,
        Color eyeColor,
        Color skinColor,
        List<Marking> markings,
        bool hairGradientEnabled = false,
        Color? hairGradientSecondaryColor = null,
        int hairGradientDirection = 0,
        bool facialHairGradientEnabled = false,
        Color? facialHairGradientSecondaryColor = null,
        int facialHairGradientDirection = 0,
        bool allMarkingsGradientEnabled = false,
        Color? allMarkingsGradientSecondaryColor = null,
        int allMarkingsGradientDirection = 0)
    {
        HairStyleId = hairStyleId;
        HairColor = ClampColor(hairColor);
        FacialHairStyleId = facialHairStyleId;
        FacialHairColor = ClampColor(facialHairColor);
        EyeColor = ClampColor(eyeColor);
        SkinColor = ClampColor(skinColor);
        Markings = markings;
        HairGradientEnabled = hairGradientEnabled;
        HairGradientSecondaryColor = hairGradientSecondaryColor ?? Color.White;
        HairGradientDirection = hairGradientDirection;
        FacialHairGradientEnabled = facialHairGradientEnabled;
        FacialHairGradientSecondaryColor = facialHairGradientSecondaryColor ?? Color.White;
        FacialHairGradientDirection = facialHairGradientDirection;
        AllMarkingsGradientEnabled = allMarkingsGradientEnabled;
        AllMarkingsGradientSecondaryColor = allMarkingsGradientSecondaryColor ?? Color.White;
        AllMarkingsGradientDirection = allMarkingsGradientDirection;
    }

    public HumanoidCharacterAppearance(HumanoidCharacterAppearance other) :
        this(other.HairStyleId, other.HairColor, other.FacialHairStyleId, other.FacialHairColor, other.EyeColor, other.SkinColor, new(other.Markings),
            other.HairGradientEnabled, other.HairGradientSecondaryColor, other.HairGradientDirection,
            other.FacialHairGradientEnabled, other.FacialHairGradientSecondaryColor, other.FacialHairGradientDirection,
            other.AllMarkingsGradientEnabled, other.AllMarkingsGradientSecondaryColor, other.AllMarkingsGradientDirection)
    {

    }

    public HumanoidCharacterAppearance WithHairStyleName(string newName)
    {
        return new(newName, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings,
            HairGradientEnabled, HairGradientSecondaryColor, HairGradientDirection,
            FacialHairGradientEnabled, FacialHairGradientSecondaryColor, FacialHairGradientDirection,
            AllMarkingsGradientEnabled, AllMarkingsGradientSecondaryColor, AllMarkingsGradientDirection);
    }

    public HumanoidCharacterAppearance WithHairColor(Color newColor)
    {
        return new(HairStyleId, newColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings,
            HairGradientEnabled, HairGradientSecondaryColor, HairGradientDirection,
            FacialHairGradientEnabled, FacialHairGradientSecondaryColor, FacialHairGradientDirection,
            AllMarkingsGradientEnabled, AllMarkingsGradientSecondaryColor, AllMarkingsGradientDirection);
    }

    public HumanoidCharacterAppearance WithFacialHairStyleName(string newName)
    {
        return new(HairStyleId, HairColor, newName, FacialHairColor, EyeColor, SkinColor, Markings,
            HairGradientEnabled, HairGradientSecondaryColor, HairGradientDirection,
            FacialHairGradientEnabled, FacialHairGradientSecondaryColor, FacialHairGradientDirection,
            AllMarkingsGradientEnabled, AllMarkingsGradientSecondaryColor, AllMarkingsGradientDirection);
    }

    public HumanoidCharacterAppearance WithFacialHairColor(Color newColor)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, newColor, EyeColor, SkinColor, Markings,
            HairGradientEnabled, HairGradientSecondaryColor, HairGradientDirection,
            FacialHairGradientEnabled, FacialHairGradientSecondaryColor, FacialHairGradientDirection,
            AllMarkingsGradientEnabled, AllMarkingsGradientSecondaryColor, AllMarkingsGradientDirection);
    }

    public HumanoidCharacterAppearance WithEyeColor(Color newColor)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, newColor, SkinColor, Markings,
            HairGradientEnabled, HairGradientSecondaryColor, HairGradientDirection,
            FacialHairGradientEnabled, FacialHairGradientSecondaryColor, FacialHairGradientDirection,
            AllMarkingsGradientEnabled, AllMarkingsGradientSecondaryColor, AllMarkingsGradientDirection);
    }

    public HumanoidCharacterAppearance WithSkinColor(Color newColor)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, newColor, Markings,
            HairGradientEnabled, HairGradientSecondaryColor, HairGradientDirection,
            FacialHairGradientEnabled, FacialHairGradientSecondaryColor, FacialHairGradientDirection,
            AllMarkingsGradientEnabled, AllMarkingsGradientSecondaryColor, AllMarkingsGradientDirection);
    }

    public HumanoidCharacterAppearance WithMarkings(List<Marking> newMarkings)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, newMarkings,
            HairGradientEnabled, HairGradientSecondaryColor, HairGradientDirection,
            FacialHairGradientEnabled, FacialHairGradientSecondaryColor, FacialHairGradientDirection,
            AllMarkingsGradientEnabled, AllMarkingsGradientSecondaryColor, AllMarkingsGradientDirection);
    }

    // _Horizon: Hair gradient
    public HumanoidCharacterAppearance WithHairGradient(bool enabled, Color secondaryColor, int direction)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings,
            enabled, secondaryColor, direction,
            FacialHairGradientEnabled, FacialHairGradientSecondaryColor, FacialHairGradientDirection,
            AllMarkingsGradientEnabled, AllMarkingsGradientSecondaryColor, AllMarkingsGradientDirection);
    }

    public HumanoidCharacterAppearance WithFacialHairGradient(bool enabled, Color secondaryColor, int direction)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings,
            HairGradientEnabled, HairGradientSecondaryColor, HairGradientDirection,
            enabled, secondaryColor, direction,
            AllMarkingsGradientEnabled, AllMarkingsGradientSecondaryColor, AllMarkingsGradientDirection);
    }

    public HumanoidCharacterAppearance WithAllMarkingsGradient(bool enabled, Color secondaryColor, int direction)
    {
        return new(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings,
            HairGradientEnabled, HairGradientSecondaryColor, HairGradientDirection,
            FacialHairGradientEnabled, FacialHairGradientSecondaryColor, FacialHairGradientDirection,
            enabled, secondaryColor, direction);
    }

    public static HumanoidCharacterAppearance DefaultWithSpecies(string species)
    {
        var speciesPrototype = IoCManager.Resolve<IPrototypeManager>().Index<SpeciesPrototype>(species);
        var skinColor = speciesPrototype.SkinColoration switch
        {
            HumanoidSkinColor.HumanToned => Humanoid.SkinColor.HumanSkinTone(speciesPrototype.DefaultHumanSkinTone),
            HumanoidSkinColor.Hues => speciesPrototype.DefaultSkinTone,
            HumanoidSkinColor.TintedHues => Humanoid.SkinColor.TintedHues(speciesPrototype.DefaultSkinTone),
            HumanoidSkinColor.KatunianToned => Humanoid.SkinColor.KatunianSkinTone(speciesPrototype.DefaultHumanSkinTone),
            HumanoidSkinColor.VoxFeathers => Humanoid.SkinColor.ClosestVoxColor(speciesPrototype.DefaultSkinTone),
            HumanoidSkinColor.ShelegToned => Humanoid.SkinColor.ShelegSkinTone(speciesPrototype.DefaultHumanSkinTone), // Frontier
            _ => Humanoid.SkinColor.ValidHumanSkinTone,
        };

        return new(
            HairStyles.DefaultHairStyle,
            Color.Black,
            HairStyles.DefaultFacialHairStyle,
            Color.Black,
            Color.Black,
            skinColor,
            new (),
            false, Color.White, 0,
            false, Color.White, 0,
            false, Color.White, 0
        );
    }
    private static IReadOnlyList<Color> RealisticEyeColors = new List<Color>
    {
        Color.Brown,
        Color.Gray,
        Color.Azure,
        Color.SteelBlue,
        Color.Black
    };
    public static HumanoidCharacterAppearance Random(string species, Sex sex)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var markingManager = IoCManager.Resolve<MarkingManager>();
        var hairStyles = markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.Hair, species).Keys.ToList();
        var facialHairStyles = markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.FacialHair, species).Keys.ToList();

        var newHairStyle = hairStyles.Count > 0
            ? random.Pick(hairStyles)
            : HairStyles.DefaultHairStyle.Id;

        var newFacialHairStyle = facialHairStyles.Count == 0 || sex == Sex.Female
            ? HairStyles.DefaultFacialHairStyle.Id
            : random.Pick(facialHairStyles);

        var newHairColor = random.Pick(HairStyles.RealisticHairColors);
        newHairColor = newHairColor
            .WithRed(RandomizeColor(newHairColor.R))
            .WithGreen(RandomizeColor(newHairColor.G))
            .WithBlue(RandomizeColor(newHairColor.B));

            // TODO: Add random markings

        var newEyeColor = random.Pick(RealisticEyeColors);

        var skinType = IoCManager.Resolve<IPrototypeManager>().Index<SpeciesPrototype>(species).SkinColoration;

        var newSkinColor = new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1);
        switch (skinType)
        {
            case HumanoidSkinColor.HumanToned:
                newSkinColor = Humanoid.SkinColor.HumanSkinTone(random.Next(0, 101));
                break;
            case HumanoidSkinColor.Hues:
                break;
            case HumanoidSkinColor.TintedHues:
                newSkinColor = Humanoid.SkinColor.ValidTintedHuesSkinTone(newSkinColor);
                break;
            case HumanoidSkinColor.VoxFeathers:
                newSkinColor = Humanoid.SkinColor.ProportionalVoxColor(newSkinColor);
                break;
        }

        return new HumanoidCharacterAppearance(newHairStyle, newHairColor, newFacialHairStyle, newHairColor, newEyeColor, newSkinColor, new(),
            false, Color.White, 0, false, Color.White, 0, false, Color.White, 0);

        float RandomizeColor(float channel)
        {
            return MathHelper.Clamp01(channel + random.Next(-25, 25) / 100f);
        }
    }

    public static Color ClampColor(Color color)
    {
        return new(color.RByte, color.GByte, color.BByte);
    }

    /// <summary>
    /// Horizon: Quantize color to a limited number of levels per channel so only displayable colors are shown in gradient sliders/preview.
    /// </summary>
    public static Color QuantizeGradientColor(Color c, int levelsPerChannel = 8)
    {
        if (levelsPerChannel <= 1) return c;
        var step = 255f / (levelsPerChannel - 1);
        byte R = (byte) Math.Clamp((int) (Math.Round(c.RByte / step) * step), 0, 255);
        byte G = (byte) Math.Clamp((int) (Math.Round(c.GByte / step) * step), 0, 255);
        byte B = (byte) Math.Clamp((int) (Math.Round(c.BByte / step) * step), 0, 255);
        return new Color(R, G, B, c.AByte);
    }

    public static HumanoidCharacterAppearance EnsureValid(HumanoidCharacterAppearance appearance, string species, Sex sex)
    {
        var hairStyleId = appearance.HairStyleId;
        var facialHairStyleId = appearance.FacialHairStyleId;

        var hairColor = ClampColor(appearance.HairColor);
        var facialHairColor = ClampColor(appearance.FacialHairColor);
        var eyeColor = ClampColor(appearance.EyeColor);

        var proto = IoCManager.Resolve<IPrototypeManager>();
        var markingManager = IoCManager.Resolve<MarkingManager>();

        if (!markingManager.MarkingsByCategory(MarkingCategories.Hair).ContainsKey(hairStyleId))
        {
            hairStyleId = HairStyles.DefaultHairStyle;
        }

        if (!markingManager.MarkingsByCategory(MarkingCategories.FacialHair).ContainsKey(facialHairStyleId))
        {
            facialHairStyleId = HairStyles.DefaultFacialHairStyle;
        }

        var markingSet = new MarkingSet();
        var skinColor = appearance.SkinColor;
        if (proto.TryIndex(species, out SpeciesPrototype? speciesProto))
        {
            markingSet = new MarkingSet(appearance.Markings, speciesProto.MarkingPoints, markingManager, proto);
            markingSet.EnsureValid(markingManager);

            if (!Humanoid.SkinColor.VerifySkinColor(speciesProto.SkinColoration, skinColor))
            {
                skinColor = Humanoid.SkinColor.ValidSkinTone(speciesProto.SkinColoration, skinColor);
            }

            markingSet.EnsureSpecies(species, skinColor, markingManager);
            markingSet.EnsureSexes(sex, markingManager);
        }

        return new HumanoidCharacterAppearance(
            hairStyleId,
            hairColor,
            facialHairStyleId,
            facialHairColor,
            eyeColor,
            skinColor,
            markingSet.GetForwardEnumerator().ToList(),
            appearance.HairGradientEnabled, appearance.HairGradientSecondaryColor, appearance.HairGradientDirection,
            appearance.FacialHairGradientEnabled, appearance.FacialHairGradientSecondaryColor, appearance.FacialHairGradientDirection,
            appearance.AllMarkingsGradientEnabled, appearance.AllMarkingsGradientSecondaryColor, appearance.AllMarkingsGradientDirection);
    }

    public bool MemberwiseEquals(ICharacterAppearance maybeOther)
    {
        if (maybeOther is not HumanoidCharacterAppearance other) return false;
        if (HairStyleId != other.HairStyleId) return false;
        if (!HairColor.Equals(other.HairColor)) return false;
        if (FacialHairStyleId != other.FacialHairStyleId) return false;
        if (!FacialHairColor.Equals(other.FacialHairColor)) return false;
        if (!EyeColor.Equals(other.EyeColor)) return false;
        if (!SkinColor.Equals(other.SkinColor)) return false;
        if (!Markings.SequenceEqual(other.Markings)) return false;
        if (HairGradientEnabled != other.HairGradientEnabled) return false;
        if (!HairGradientSecondaryColor.Equals(other.HairGradientSecondaryColor)) return false;
        if (HairGradientDirection != other.HairGradientDirection) return false;
        if (FacialHairGradientEnabled != other.FacialHairGradientEnabled) return false;
        if (!FacialHairGradientSecondaryColor.Equals(other.FacialHairGradientSecondaryColor)) return false;
        if (FacialHairGradientDirection != other.FacialHairGradientDirection) return false;
        if (AllMarkingsGradientEnabled != other.AllMarkingsGradientEnabled) return false;
        if (!AllMarkingsGradientSecondaryColor.Equals(other.AllMarkingsGradientSecondaryColor)) return false;
        if (AllMarkingsGradientDirection != other.AllMarkingsGradientDirection) return false;
        return true;
    }

    public bool Equals(HumanoidCharacterAppearance? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return HairStyleId == other.HairStyleId &&
               HairColor.Equals(other.HairColor) &&
               FacialHairStyleId == other.FacialHairStyleId &&
               FacialHairColor.Equals(other.FacialHairColor) &&
               EyeColor.Equals(other.EyeColor) &&
               SkinColor.Equals(other.SkinColor) &&
               Markings.SequenceEqual(other.Markings) &&
               HairGradientEnabled == other.HairGradientEnabled &&
               HairGradientSecondaryColor.Equals(other.HairGradientSecondaryColor) &&
               HairGradientDirection == other.HairGradientDirection &&
               FacialHairGradientEnabled == other.FacialHairGradientEnabled &&
               FacialHairGradientSecondaryColor.Equals(other.FacialHairGradientSecondaryColor) &&
               FacialHairGradientDirection == other.FacialHairGradientDirection &&
               AllMarkingsGradientEnabled == other.AllMarkingsGradientEnabled &&
               AllMarkingsGradientSecondaryColor.Equals(other.AllMarkingsGradientSecondaryColor) &&
               AllMarkingsGradientDirection == other.AllMarkingsGradientDirection;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is HumanoidCharacterAppearance other && Equals(other);
    }

    public override int GetHashCode()
    {
        var h = HashCode.Combine(HairStyleId, HairColor, FacialHairStyleId, FacialHairColor, EyeColor, SkinColor, Markings);
        var g1 = HashCode.Combine(HairGradientEnabled, HairGradientSecondaryColor, HairGradientDirection,
            FacialHairGradientEnabled, FacialHairGradientSecondaryColor, FacialHairGradientDirection);
        var g2 = HashCode.Combine(AllMarkingsGradientEnabled, AllMarkingsGradientSecondaryColor, AllMarkingsGradientDirection);
        return HashCode.Combine(h, g1, g2);
    }

    public HumanoidCharacterAppearance Clone()
    {
        return new(this);
    }
}
