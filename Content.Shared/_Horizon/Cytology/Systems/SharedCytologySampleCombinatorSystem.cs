using Content.Shared._Horizon.Cytology.Components;
using Content.Shared.Preferences;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Cytology.Systems;

public sealed class SharedCytologySampleCombinator
{
    public const string PetriDishSlotName = "petriDishSlot";
    public const string DiskSlot1Name = "diskSlot1";
    public const string DiskSlot2Name = "diskSlot2";
    public const string DiskSlot3Name = "diskSlot3";

}

[Serializable, NetSerializable]
public sealed class SampleCombinatorCellSampleInfo
{
    public readonly int Index;
    public readonly string DisplayName;
    public readonly string ProtoID;
    public readonly float GrowProgress;
    public readonly HumanoidCharacterProfile? StoredProfile;

    public SampleCombinatorCellSampleInfo(int index, string displayName, string protoID, float growProgress, HumanoidCharacterProfile? storedProfile)
    {
        Index = index;
        DisplayName = displayName;
        ProtoID = protoID;
        GrowProgress = growProgress;
        StoredProfile = storedProfile;
    }
}

[Serializable, NetSerializable]
public sealed class CytologySampleCombinatorBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly List<CellSample>? CellSamples;
    public readonly List<string>? CellSamplesNames; //TODO определенно переделать
    public readonly List<string>? AvailableDiskPrototypes;

    public CytologySampleCombinatorBoundUserInterfaceState(List<CellSample>? cellSamples, List<string>? cellSamplesNames, List<string>? availableDiskPrototypes = null)
    {
        CellSamples = cellSamples;
        CellSamplesNames = cellSamplesNames;
        AvailableDiskPrototypes = availableDiskPrototypes;
    }
}

[Serializable, NetSerializable]
public enum CytologySampleCombinatorUiKey
{
    Key
}

/// <summary>
/// Active on OnDeletePressed invoke in SampleItem
/// </summary>
[Serializable, NetSerializable]
public sealed class CytologySampleCombinatorDeleteSampleMessage : BoundUserInterfaceMessage
{
    public readonly int SampleIndex;

    public CytologySampleCombinatorDeleteSampleMessage(int sampleIndex)
    {
        SampleIndex = sampleIndex;
    }
}

/// <summary>
/// Active on OnSavePressed invoke in CytologySampleCombinatorWindow
/// </summary>
[Serializable, NetSerializable]
public sealed class CytologySampleCombinatorUpdateProfileMessage : BoundUserInterfaceMessage
{
    public readonly int SampleIndex;
    public readonly HumanoidCharacterProfile? Profile;

    public CytologySampleCombinatorUpdateProfileMessage(int sampleIndex, HumanoidCharacterProfile? profile)
    {
        SampleIndex = sampleIndex;
        Profile = profile;
    }
}

/// <summary>
/// Also active on OnSavePressed invoke in CytologySampleCombinatorWindow
/// </summary>
[Serializable, NetSerializable]
public sealed class CytologySampleCombinatorUpdateDisksMessage : BoundUserInterfaceMessage
{
    public readonly int SampleIndex;
    public readonly List<string> SelectedDiskPrototypes;

    public CytologySampleCombinatorUpdateDisksMessage(int sampleIndex, List<string> selectedDiskPrototypes)
    {
        SampleIndex = sampleIndex;
        SelectedDiskPrototypes = selectedDiskPrototypes;
    }
}

