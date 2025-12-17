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

    public CellSample(string protoID, float growProgress = 0f, HumanoidCharacterProfile? storedProfile = null)
    {
        ProtoID = protoID;
        GrowProgress = growProgress;
        StoredProfile = storedProfile;
    }
}
