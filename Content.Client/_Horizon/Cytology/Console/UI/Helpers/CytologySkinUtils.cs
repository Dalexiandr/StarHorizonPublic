using Content.Shared.Humanoid;

namespace Content.Client._Horizon.Cytology.Console.UI.Helpers;

public static class CytologySkinUtils
{
    public static Color GetSkinColor(HumanoidSkinColor type, float sliderValue, Color selectorColor)
    {
        return type switch
        {
            HumanoidSkinColor.HumanToned => SkinColor.HumanSkinTone((int)sliderValue),
            HumanoidSkinColor.Hues => selectorColor,
            HumanoidSkinColor.TintedHues => SkinColor.TintedHues(selectorColor),
            HumanoidSkinColor.KatunianToned => SkinColor.KatunianSkinTone((int)sliderValue / 2),
            HumanoidSkinColor.VoxFeathers => SkinColor.ClosestVoxColor(selectorColor),
            HumanoidSkinColor.ShelegToned => SkinColor.ShelegSkinTone((int)sliderValue),
            _ => selectorColor
        };
    }
}
