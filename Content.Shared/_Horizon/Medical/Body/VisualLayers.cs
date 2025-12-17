using Content.Shared.Humanoid;

namespace Content.Shared._Horizon.Medical.Body;
public static class VisualLayers
{
    public static HumanoidVisualLayers GetLayer(string slotId)
    {
        return slotId switch
        {
            "left arm" => HumanoidVisualLayers.LArm,
            "right arm" => HumanoidVisualLayers.RArm,
            "left hand" => HumanoidVisualLayers.LHand,
            "right hand" => HumanoidVisualLayers.RHand,
            "left leg" => HumanoidVisualLayers.LLeg,
            "right leg" => HumanoidVisualLayers.RLeg,
            "left foot" => HumanoidVisualLayers.LFoot,
            "right foot" => HumanoidVisualLayers.RFoot,
            "tail" => HumanoidVisualLayers.Tail,
            _ => HumanoidVisualLayers.Other,
        };
    }

    public static string GetSlotId(HumanoidVisualLayers layer)
    {
        return layer switch
        {
            HumanoidVisualLayers.LArm => "left arm",
            HumanoidVisualLayers.RArm => "right arm",
            HumanoidVisualLayers.LHand => "left hand",
            HumanoidVisualLayers.RHand => "right hand",
            HumanoidVisualLayers.LLeg => "left leg",
            HumanoidVisualLayers.RLeg => "right leg",
            HumanoidVisualLayers.LFoot => "left foot",
            HumanoidVisualLayers.RFoot => "right foot",
            HumanoidVisualLayers.Tail => "tail",
            _ => "other",
        };
    }
}
