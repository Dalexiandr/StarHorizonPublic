using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Cytology.Components;


/// <summary>
///     Stores the cell shot along with the growth parameter, which changes during growing
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class CellSample
{
    [DataField]
    public string ProtoID;

    [DataField]
    public float GrowProgress;

    [DataField]
    public HumanoidCharacterProfile? StoredProfile;

    /// <summary>
    /// List of selected disk entity prototypes for this cell sample.
    /// </summary>
    [DataField]
    public List<string>? SelectedDiskPrototypes;

    public CellSample(string protoID, float growProgress = 0f, HumanoidCharacterProfile? storedProfile = null, List<string>? selectedDiskPrototypes = null)
    {
        ProtoID = protoID;
        GrowProgress = growProgress;
        StoredProfile = storedProfile;
        SelectedDiskPrototypes = selectedDiskPrototypes;
    }

    public CellSample Clone()
    {
        return new CellSample
        {
            ProtoID = this.ProtoID,
            GrowProgress = this.GrowProgress,
            StoredProfile = this.StoredProfile,
            SelectedDiskPrototypes = this.SelectedDiskPrototypes
        };
    }
}
